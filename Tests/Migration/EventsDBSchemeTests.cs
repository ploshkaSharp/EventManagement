using Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using EventManagement.Events.Infrastructure.Data;
using EventManagement.Events.Infrastructure.Repositories;

namespace EventManagement.IntegrationTests;

/// <summary>
/// Интеграционный тест для проверки схемы базы данных сервиса Events
/// </summary>
public class EventsDatabaseSchemaTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;
    private ServiceProvider _serviceProvider;
    private string _connectionString;

    public EventsDatabaseSchemaTests()
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("events_test")
            .WithUsername("test_user")
            .WithPassword("test_password")
            .WithCleanUp(true)
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();
        _connectionString = _postgresContainer.GetConnectionString();

        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(_connectionString));
        services.AddLogging();

        _serviceProvider = services.BuildServiceProvider();

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgresContainer.DisposeAsync();
        await _serviceProvider.DisposeAsync();
    }

    [Fact]
    public async Task EventsDatabaseSchema_ShouldHaveTablesAndForeignKeys()
    {
        using var scope = _serviceProvider.CreateScope();
        await using var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();
        await using var connection = context.Database.GetDbConnection();

        await connection.OpenAsync();

        try
        {
            // Проверка существования таблицы Events
            var eventsExists = await TableExistsAsync(connection, "Events");
            Assert.True(eventsExists, "Таблица Events не найдена");

            // Проверка существования таблицы ProcessedBookings
            var processedBookingsExists = await TableExistsAsync(connection, "ProcessedBookings");
            Assert.True(processedBookingsExists, "Таблица ProcessedBookings не найдена");

            // Проверка наличия колонок в таблице Events
            var eventColumns = await GetTableColumnsAsync(connection, "Events");
            var requiredEventColumns = new[] { "Id", "Title", "Description", "StartAt", "EndAt", "TotalSeats", "AvailableSeats" };
            foreach (var column in requiredEventColumns)
            {
                Assert.Contains(column, eventColumns);
            }

            // Проверка наличия колонок в таблице ProcessedBookings
            var processedBookingColumns = await GetTableColumnsAsync(connection, "ProcessedBookings");
            var requiredProcessedBookingColumns = new[] { "Id", "BookingId", "EventId", "UserId", "ProcessedAt", "Success", "FailureReason", "AvailableSeats" };
            foreach (var column in requiredProcessedBookingColumns)
            {
                Assert.Contains(column, processedBookingColumns);
            }

            // Проверка уникального индекса на BookingId
            var uniqueIndexExists = await UniqueIndexExistsAsync(connection, "ProcessedBookings", "BookingId");
            Assert.True(uniqueIndexExists, "Уникальный индекс на BookingId в таблице ProcessedBookings не найден");

            // Проверка индексов на EventId и ProcessedAt
            var indexOnEventId = await IndexExistsAsync(connection, "ProcessedBookings", "EventId");
            Assert.True(indexOnEventId, "Индекс на EventId в таблице ProcessedBookings не найден");

            // Проверка индексов в таблице Events
            var eventIndexes = await GetTableIndexesAsync(connection, "Events");
            Assert.Contains("IX_Events_StartAt", eventIndexes);
            Assert.Contains("IX_Events_Title", eventIndexes);
        }
        finally
        {
            await connection.CloseAsync();
        }
    }

    [Fact]
    public async Task EventsDatabaseSchema_ShouldHaveCorrectColumnTypes()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();
        var connection = context.Database.GetDbConnection();
        await connection.OpenAsync();

        try
        {
            // Проверка типов колонок таблицы Events
            var eventColumnTypes = await GetColumnTypesAsync(connection, "Events");
            Assert.Equal("uuid", eventColumnTypes["Id"]);
            Assert.Equal("character varying", eventColumnTypes["Title"]);
            Assert.Equal("character varying", eventColumnTypes["Description"]);
            Assert.Equal("timestamp with time zone", eventColumnTypes["StartAt"]);
            Assert.Equal("timestamp with time zone", eventColumnTypes["EndAt"]);
            Assert.Equal("integer", eventColumnTypes["TotalSeats"]);
            Assert.Equal("integer", eventColumnTypes["AvailableSeats"]);

            // Проверка типов колонок таблицы ProcessedBookings
            var processedBookingColumnTypes = await GetColumnTypesAsync(connection, "ProcessedBookings");
            Assert.Equal("uuid", processedBookingColumnTypes["Id"]);
            Assert.Equal("uuid", processedBookingColumnTypes["BookingId"]);
            Assert.Equal("uuid", processedBookingColumnTypes["EventId"]);
            Assert.Equal("uuid", processedBookingColumnTypes["UserId"]);
            Assert.Equal("timestamp with time zone", processedBookingColumnTypes["ProcessedAt"]);
            Assert.Equal("boolean", processedBookingColumnTypes["Success"]);
            Assert.Equal("character varying", processedBookingColumnTypes["FailureReason"]);
            Assert.Equal("integer", processedBookingColumnTypes["AvailableSeats"]);
        }
        finally
        {
            await connection.CloseAsync();
        }
    }

    #region Helper Methods

    private async Task<bool> TableExistsAsync(System.Data.Common.DbConnection connection, string tableName)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT EXISTS (
                SELECT 1 
                FROM information_schema.tables 
                WHERE table_name = @tableName
                AND table_schema = 'public'
            )";
        var param = command.CreateParameter();
        param.ParameterName = "@tableName";
        param.Value = tableName;
        command.Parameters.Add(param);

        return (bool)await command.ExecuteScalarAsync();
    }

    private async Task<List<string>> GetTableColumnsAsync(System.Data.Common.DbConnection connection, string tableName)
    {
        var columns = new List<string>();
        await using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT column_name 
            FROM information_schema.columns 
            WHERE table_name = @tableName 
            AND table_schema = 'public'
            ORDER BY ordinal_position";
        var param = command.CreateParameter();
        param.ParameterName = "@tableName";
        param.Value = tableName;
        command.Parameters.Add(param);

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            columns.Add(reader.GetString(0));
        }
        return columns;
    }

    private async Task<Dictionary<string, string>> GetColumnTypesAsync(System.Data.Common.DbConnection connection, string tableName)
    {
        var columnTypes = new Dictionary<string, string>();
        await using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT column_name, data_type 
            FROM information_schema.columns 
            WHERE table_name = @tableName 
            AND table_schema = 'public'";
        var param = command.CreateParameter();
        param.ParameterName = "@tableName";
        param.Value = tableName;
        command.Parameters.Add(param);

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            columnTypes[reader.GetString(0)] = reader.GetString(1);
        }
        return columnTypes;
    }

    private async Task<bool> UniqueIndexExistsAsync(System.Data.Common.DbConnection connection, string tableName, string columnName)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT EXISTS (
                SELECT 1
                FROM pg_indexes
                WHERE schemaname = 'public'
                AND tablename = @tableName
                AND indexdef LIKE '%UNIQUE%'
                AND indexdef LIKE '%' || @columnName || '%'
            )";
        var paramTable = command.CreateParameter();
        paramTable.ParameterName = "@tableName";
        paramTable.Value = tableName;
        command.Parameters.Add(paramTable);

        var paramColumn = command.CreateParameter();
        paramColumn.ParameterName = "@columnName";
        paramColumn.Value = columnName;
        command.Parameters.Add(paramColumn);

        return (bool)await command.ExecuteScalarAsync();
    }

    private async Task<bool> IndexExistsAsync(System.Data.Common.DbConnection connection, string tableName, string columnName)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT EXISTS (
                SELECT 1
                FROM pg_indexes
                WHERE schemaname = 'public'
                AND tablename = @tableName
                AND indexdef LIKE '%' || @columnName || '%'
            )";
        var paramTable = command.CreateParameter();
        paramTable.ParameterName = "@tableName";
        paramTable.Value = tableName;
        command.Parameters.Add(paramTable);

        var paramColumn = command.CreateParameter();
        paramColumn.ParameterName = "@columnName";
        paramColumn.Value = columnName;
        command.Parameters.Add(paramColumn);

        return (bool)await command.ExecuteScalarAsync();
    }

    private async Task<List<string>> GetTableIndexesAsync(System.Data.Common.DbConnection connection, string tableName)
    {
        var indexes = new List<string>();
        await using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT indexname
            FROM pg_indexes
            WHERE schemaname = 'public'
            AND tablename = @tableName";
        var param = command.CreateParameter();
        param.ParameterName = "@tableName";
        param.Value = tableName;
        command.Parameters.Add(param);

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            indexes.Add(reader.GetString(0));
        }
        return indexes;
    }

    #endregion
}
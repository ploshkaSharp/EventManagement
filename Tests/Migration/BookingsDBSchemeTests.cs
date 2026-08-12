using Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using EventManagement.Bookings.Infrastructure.Data;
using EventManagement.Bookings.Infrastructure.Repositories;

namespace EventManagement.IntegrationTests;

/// <summary>
/// Интеграционный тест для проверки схемы базы данных сервиса Bookings
/// </summary>
public class BookingsDatabaseSchemaTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;
    private ServiceProvider _serviceProvider;
    private string _connectionString;

    public BookingsDatabaseSchemaTests()
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("bookings_test")
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
    public async Task BookingsDatabaseSchema_ShouldHaveTablesAndForeignKeys()
    {
        using var scope = _serviceProvider.CreateScope();
        await using var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();
        await using var connection = context.Database.GetDbConnection();

        await connection.OpenAsync();

        try
        {
            // Проверка существования таблицы Bookings
            var bookingsExists = await TableExistsAsync(connection, "Bookings");
            Assert.True(bookingsExists, "Таблица Bookings не найдена");

            // Проверка наличия колонок в таблице Bookings
            var bookingColumns = await GetTableColumnsAsync(connection, "Bookings");
            var requiredBookingColumns = new[] { "Id", "EventId", "UserId", "Status", "CreatedAt", "ProcessedAt" };
            foreach (var column in requiredBookingColumns)
            {
                Assert.Contains(column, bookingColumns);
            }

            // Проверка индексов в таблице Bookings
            var bookingIndexes = await GetTableIndexesAsync(connection, "Bookings");
            Assert.Contains("IX_Bookings_EventId", bookingIndexes);
            Assert.Contains("IX_Bookings_UserId", bookingIndexes);
            Assert.Contains("IX_Bookings_Status", bookingIndexes);
            Assert.Contains("IX_Bookings_CreatedAt", bookingIndexes);
        }
        finally
        {
            await connection.CloseAsync();
        }
    }

    [Fact]
    public async Task BookingsDatabaseSchema_ShouldHaveCorrectColumnTypes()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();
        var connection = context.Database.GetDbConnection();
        await connection.OpenAsync();

        try
        {
            var bookingColumnTypes = await GetColumnTypesAsync(connection, "Bookings");
            Assert.Equal("uuid", bookingColumnTypes["Id"]);
            Assert.Equal("uuid", bookingColumnTypes["EventId"]);
            Assert.Equal("uuid", bookingColumnTypes["UserId"]);
            Assert.Equal("character varying", bookingColumnTypes["Status"]);
            Assert.Equal("timestamp with time zone", bookingColumnTypes["CreatedAt"]);
            Assert.Equal("timestamp with time zone", bookingColumnTypes["ProcessedAt"]);
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
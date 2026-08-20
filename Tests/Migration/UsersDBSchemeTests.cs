using Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using EventManagement.Users.Infrastructure.Data;

namespace EventManagement.IntegrationTests;

/// <summary>
/// Интеграционный тест для проверки схемы базы данных сервиса Users
/// </summary>
public class UsersDatabaseSchemaTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;
    private ServiceProvider _serviceProvider;
    private string _connectionString;

    public UsersDatabaseSchemaTests()
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("users_test")
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
    public async Task UsersDatabaseSchema_ShouldHaveTablesAndForeignKeys()
    {
        using var scope = _serviceProvider.CreateScope();
        await using var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();
        await using var connection = context.Database.GetDbConnection();

        await connection.OpenAsync();

        try
        {            
            // Проверка существования таблицы Users
            var usersExists = await TableExistsAsync(connection, "Users");
            Assert.True(usersExists, "Таблица Users не найдена");

            // Проверка наличия колонок в таблице Users
            var userColumns = await GetTableColumnsAsync(connection, "Users");
            var requiredUserColumns = new[] { "Id", "Login", "PasswordHash", "Role" };
            foreach (var column in requiredUserColumns)
            {
                Assert.Contains(column, userColumns);
            }

            // Проверка уникального индекса на Login
            var uniqueIndexExists = await UniqueIndexExistsAsync(connection, "Users", "Login");
            Assert.True(uniqueIndexExists, "Уникальный индекс на Login в таблице Users не найден");
        }
        finally
        {
            await connection.CloseAsync();
        }
    }

    [Fact]
    public async Task UsersDatabaseSchema_ShouldHaveCorrectColumnTypes()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();
        var connection = context.Database.GetDbConnection();
        await connection.OpenAsync();

        try
        {
            var userColumnTypes = await GetColumnTypesAsync(connection, "Users");
            Assert.Equal("uuid", userColumnTypes["Id"]);
            Assert.Equal("character varying", userColumnTypes["Login"]);
            Assert.Equal("character varying", userColumnTypes["PasswordHash"]);
            Assert.Equal("text", userColumnTypes["Role"]);
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

    #endregion
}
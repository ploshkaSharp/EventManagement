using Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using EventManagement.Infrastructure.Data;

namespace EventManagement.IntegrationTests;

/// <summary>
/// Интеграционный тест для проверки схемы базы данных
/// </summary>
public class DatabaseSchemaTests : IAsyncLifetime
{
  private readonly PostgreSqlContainer _postgresContainer;
  private ServiceProvider _serviceProvider;
  private string _connectionString;

  /// <summary>
  /// Контейнер с БД
  /// </summary>
  public DatabaseSchemaTests()
  {
    _postgresContainer = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("eventmanagement_test")
        .WithUsername("test_user")
        .WithPassword("test_password")
        .WithCleanUp(true)
        .Build();
  }

  public async Task InitializeAsync()
  {
    // Запуск контейнера PostgreSQL
    await _postgresContainer.StartAsync();
    _connectionString = _postgresContainer.GetConnectionString();

    // DI
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
  public async Task DatabaseSchema_ShouldHaveTablesAndForeignKeys()
  {

    // Arrange
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

      // Проверка существования таблицы Bookings
      var bookingsExists = await TableExistsAsync(connection, "Bookings");
      Assert.True(bookingsExists, "Таблица Bookings не найдена");

      // Проверка наличия колонки EventId в таблице Bookings
      var columnExists = await ColumnExistsAsync(connection, "Bookings", "EventId");
      Assert.True(columnExists, "Колонка EventId не найдена в таблице Bookings");

      // Проверка наличия внешнего ключа между Bookings.EventId и Events.Id
      var foreignKeyExists = await ForeignKeyExistsAsync(connection, "Bookings", "Events", "EventId");
      Assert.True(foreignKeyExists, "Внешний ключ между Bookings.EventId и Events.Id не найден");
    }
    finally
    {
      await connection.CloseAsync();
    }
  }

  [Fact]
  public async Task DatabaseSchema_ShouldHaveAllRequiredColumns()
  {
    // Arrange
    using var scope = _serviceProvider.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await context.Database.EnsureDeletedAsync();
    await context.Database.MigrateAsync();
    var connection = context.Database.GetDbConnection();
    await connection.OpenAsync();

    try
    {
      // Проверка колонок таблицы Events
      var eventColumns = await GetTableColumnsAsync(connection, "Events");
      var requiredEventColumns = new[] { "Id", "Title", "Description", "StartAt", "EndAt", "TotalSeats", "AvailableSeats" };
      foreach (var column in requiredEventColumns)
      {
        Assert.Contains(column, eventColumns);
      }

      // Проверка колонок таблицы Bookings
      var bookingColumns = await GetTableColumnsAsync(connection, "Bookings");
      var requiredBookingColumns = new[] { "Id", "EventId", "Status", "CreatedAt", "ProcessedAt" };
      foreach (var column in requiredBookingColumns)
      {
        Assert.Contains(column, bookingColumns);
      }
    }
    finally
    {
      await connection.CloseAsync();
    }
  }

  [Fact]
  public async Task DatabaseSchema_ShouldHaveCorrectColumnTypes()
  {
    // Arrange
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

      // Проверка типов колонок таблицы Bookings
      var bookingColumnTypes = await GetColumnTypesAsync(connection, "Bookings");
      Assert.Equal("uuid", bookingColumnTypes["Id"]);
      Assert.Equal("uuid", bookingColumnTypes["EventId"]);
      Assert.Equal("character varying", bookingColumnTypes["Status"]);
      Assert.Equal("timestamp with time zone", bookingColumnTypes["CreatedAt"]);
      Assert.Equal("timestamp with time zone", bookingColumnTypes["ProcessedAt"]);
    }
    finally
    {
      await connection.CloseAsync();
    }
  }

  /// <summary>
  /// Вернуть строку со именами всех таблиц
  /// </summary>
  private async Task<string> AllTablesAsync(System.Data.Common.DbConnection connection)
  {
    await using var command = connection.CreateCommand();
    command.CommandText = @"            
                SELECT STRING_AGG(table_name, ', ') AS tables
                FROM information_schema.tables
                WHERE table_schema = 'public'
                AND table_type = 'BASE TABLE'
            ";
    return (string)await command.ExecuteScalarAsync();
  }

  /// <summary>
  /// Проверка существования таблицы
  /// </summary>
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

  /// <summary>
  /// Проверка существования колонки в таблице
  /// </summary>
  private async Task<bool> ColumnExistsAsync(System.Data.Common.DbConnection connection, string tableName, string columnName)
  {
    await using var command = connection.CreateCommand();
    command.CommandText = @"
            SELECT EXISTS (
                SELECT 1 
                FROM information_schema.columns 
                WHERE table_name = @tableName 
                AND column_name = @columnName
                AND table_schema = 'public'
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

  /// <summary>
  /// Проверяка существования внешнего ключа между таблицами
  /// </summary>
  private async Task<bool> ForeignKeyExistsAsync(System.Data.Common.DbConnection connection, string childTable, string parentTable, string childColumn)
  {
    await using var command = connection.CreateCommand();
    command.CommandText = @"
            SELECT EXISTS (
                SELECT 1
                FROM information_schema.key_column_usage kcu
                JOIN information_schema.table_constraints tc
                    ON tc.constraint_name = kcu.constraint_name
                    AND tc.table_schema = kcu.table_schema
                JOIN information_schema.referential_constraints rc
                    ON rc.constraint_name = tc.constraint_name
                    AND rc.constraint_schema = tc.table_schema
                WHERE tc.constraint_type = 'FOREIGN KEY'
                AND tc.table_schema = 'public'
                AND tc.table_name = @childTable
                AND kcu.column_name = @childColumn
                AND rc.unique_constraint_name IN (
                    SELECT constraint_name
                    FROM information_schema.key_column_usage
                    WHERE table_name = @parentTable
                    AND table_schema = 'public'
                )
            )";
    var paramChildTable = command.CreateParameter();
    paramChildTable.ParameterName = "@childTable";
    paramChildTable.Value = childTable;
    command.Parameters.Add(paramChildTable);

    var paramChildColumn = command.CreateParameter();
    paramChildColumn.ParameterName = "@childColumn";
    paramChildColumn.Value = childColumn;
    command.Parameters.Add(paramChildColumn);

    var paramParentTable = command.CreateParameter();
    paramParentTable.ParameterName = "@parentTable";
    paramParentTable.Value = parentTable;
    command.Parameters.Add(paramParentTable);

    return (bool)await command.ExecuteScalarAsync();
  }

  /// <summary>
  /// Получить список колонок таблицы
  /// </summary>
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

  /// <summary>
  /// Получить типы колонок таблицы
  /// </summary>
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

}
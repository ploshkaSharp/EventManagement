using Xunit;
using Testcontainers.PostgreSql;

namespace EventMangement.IntegrationTests.Base;

/// <summary>
/// 
/// </summary>
public abstract class IntegrationTestBase : IAsyncLifetime
{
  /// <summary>
  /// 
  /// </summary>
  protected readonly PostgreSqlContainer _postgresContainer;
  /// <summary>
  /// 
  /// </summary>
  /// 
  protected IntegrationTestBase()
  {
    _postgresContainer = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("eventmanagement_test")
        .WithUsername("test_user")
        .WithPassword("test_password")
        .WithCleanUp(true)
        .Build();
  }

  /// <summary>
  /// 
  /// </summary>
  /// <returns></returns>
  public virtual async Task InitializeAsync()
  {
    await _postgresContainer.StartAsync();
  }

  /// <summary>
  /// 
  /// </summary>
  /// <returns></returns>
  public virtual async Task DisposeAsync()
  {
    await _postgresContainer.DisposeAsync();
  }
}

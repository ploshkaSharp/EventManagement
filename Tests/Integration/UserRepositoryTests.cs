using Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using EventManagement.Users.Application.Ports;
using EventManagement.Users.Domain.Entities;
using EventManagement.Users.Domain.Enums;
using EventManagement.Users.Infrastructure.Data;
using EventManagement.Users.Infrastructure.Repositories;

namespace EventManagement.IntegrationTests.Repositories;

/// <summary>
/// Базовый класс для интеграционных тестов репозитория пользователей
/// </summary>
public abstract class UserIntegrationTestBase : IAsyncLifetime
{
    protected PostgreSqlContainer _postgresContainer;
    protected ServiceProvider _serviceProvider;
    protected string _connectionString;

    protected UserIntegrationTestBase()
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
        await _postgresContainer.StartAsync();
        _connectionString = _postgresContainer.GetConnectionString();

        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(_connectionString));
        services.AddScoped<IUserRepository, UserRepository>();
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

    protected async Task ResetDatabaseAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();
    }
}

/// <summary>
/// Интеграционные тесты репозитория пользователей
/// </summary>
public class UserRepositoryTests : UserIntegrationTestBase
{
    [Fact]
    public async Task CreateAsync_ShouldAddUserToDatabase()
    {
        using var scope = _serviceProvider.CreateScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        var user = new User("testuser", "hashedpassword", Role.User);
        var result = await userRepository.CreateAsync(user);

        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("testuser", result.Login);
        Assert.Equal(Role.User, result.Role);
    }

    [Fact]
    public async Task CreateAsync_DuplicateLogin_ShouldThrowDbUpdateException()
    {
        using var scope = _serviceProvider.CreateScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        var user1 = new User("testuser", "hash1", Role.User);
        await userRepository.CreateAsync(user1);

        var user2 = new User("testuser", "hash2", Role.User);

        await Assert.ThrowsAsync<DbUpdateException>(async () =>
            await userRepository.CreateAsync(user2));
    }

    [Fact]
    public async Task GetByLoginAsync_ShouldReturnUser()
    {
        using var scope = _serviceProvider.CreateScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        var user = new User("testuser", "hashedpassword", Role.User);
        await userRepository.CreateAsync(user);

        var result = await userRepository.GetByLoginAsync("testuser");

        Assert.NotNull(result);
        Assert.Equal("testuser", result.Login);
    }

    [Fact]
    public async Task GetByLoginAsync_NonExistent_ShouldReturnNull()
    {
        using var scope = _serviceProvider.CreateScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        var result = await userRepository.GetByLoginAsync("nonexistent");

        Assert.Null(result);
    }
}
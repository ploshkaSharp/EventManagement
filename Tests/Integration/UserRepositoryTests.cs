using Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using EventManagement.Domain.Entities;
using EventManagement.Domain.Enums;
using EventManagement.Application.Ports;
using EventManagement.Infrastructure.Data;
using EventManagement.IntegrationTests.Base;

namespace EventManagement.IntegrationTests.Repositories;

public class UserRepositoryTests : IntegrationTestBase
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

  [Fact]
  public async Task CountActiveBookingsAsync_ShouldReturnCorrectCount()
  {
    using var scope = _serviceProvider.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

    var user = new User("testuser", "hash", Role.User);
    await userRepository.CreateAsync(user);

    var eventItem = new Event("Test", DateTime.UtcNow.AddDays(30), DateTime.UtcNow.AddDays(34), 10);
    context.Events.Add(eventItem);
    await context.SaveChangesAsync();

    var booking1 = new Booking(eventItem.Id, user.Id);
    booking1.Confirm();
    var booking2 = new Booking(eventItem.Id, user.Id);
    var booking3 = new Booking(eventItem.Id, user.Id);
    booking3.Reject();

    context.Bookings.AddRange(booking1, booking2, booking3);
    await context.SaveChangesAsync();

    var count = await userRepository.CountActiveBookingsAsync(user.Id);

    Assert.Equal(2, count); // booking1 (Confirmed) + booking2 (Pending) = 2
  }
}
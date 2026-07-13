using EventManagement.Domain.Entities;

namespace EventManagement.Application.Ports;

public interface IUserRepository
{
  Task<User?> GetByIdAsync(Guid id);
  Task<User?> GetByLoginAsync(string login);
  Task<IEnumerable<User>> GetAllAsync();
  Task<User> CreateAsync(User user);
  Task<User?> UpdateAsync(User user);
  Task<bool> DeleteAsync(Guid id);
}
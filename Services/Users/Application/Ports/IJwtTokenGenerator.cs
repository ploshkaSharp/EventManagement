namespace EventManagement.Users.Application.Ports;

public interface IJwtTokenGenerator
{
  string GenerateToken(Guid userId, string login, string role);
}
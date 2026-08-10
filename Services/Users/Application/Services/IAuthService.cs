using EventManagement.Users.Application.DTOs;

namespace EventManagement.Users.Application.Services;

public interface IAuthService
{
    Task RegisterAsync(RegisterDTO registerDto);
    Task<string> LoginAsync(LoginDTO loginDto);
}
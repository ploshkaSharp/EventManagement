using EventManagement.Application.DTOs;

namespace EventManagement.Application.Services;

public interface IAuthService
{
    Task RegisterAsync(RegisterDTO registerDto);
    Task<string> LoginAsync(LoginDTO loginDto);
}
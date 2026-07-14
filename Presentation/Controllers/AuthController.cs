using Microsoft.AspNetCore.Mvc;
using EventManagement.Application.DTOs;
using EventManagement.Application.Services;

namespace EventManagement.Presentation.Controllers;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;
    
    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }
    
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDTO registerDto)
    {
        _logger.LogInformation("Registering user {Login}", registerDto.Login);
        await _authService.RegisterAsync(registerDto);
        return NoContent();
    }
    
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDTO loginDto)
    {
        _logger.LogInformation("Login attempt for user {Login}", loginDto.Login);
        var token = await _authService.LoginAsync(loginDto);
        return Ok(new AuthResponseDTO(token));
    }
}
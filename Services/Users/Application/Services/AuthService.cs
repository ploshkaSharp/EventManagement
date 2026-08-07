using EventManagement.Users.Application.DTOs;
using EventManagement.Users.Application.Ports;
using EventManagement.Users.Domain.Entities;
using EventManagement.Users.Domain.Exceptions;

namespace EventManagement.Users.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public AuthService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task RegisterAsync(RegisterDTO registerDto)
    {
        if (string.IsNullOrWhiteSpace(registerDto.Login))
            throw new ValidationException("Login is required");

        if (string.IsNullOrWhiteSpace(registerDto.Password))
            throw new ValidationException("Password is required");

        var user = await _userRepository.GetByLoginAsync(registerDto.Login);
        if (user != null)
        {
            throw new ValidationException($"User with login '{registerDto.Login}' already exists");
        }

        var passwordHash = _passwordHasher.Hash(registerDto.Password);
        var newUser = new User(registerDto.Login, passwordHash, registerDto.Role);
        await _userRepository.CreateAsync(newUser);
    }

    public async Task<string> LoginAsync(LoginDTO loginDto)
    {
        var user = await _userRepository.GetByLoginAsync(loginDto.Login);

        if (user == null || !_passwordHasher.Verify(loginDto.Password, user.PasswordHash))
        {
            throw new NotFoundException("Invalid login or password");
        }

        return _jwtTokenGenerator.GenerateToken(user.Id, user.Login, user.Role.ToString());
    }
}
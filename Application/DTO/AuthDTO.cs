using EventManagement.Domain.Enums;

namespace EventManagement.Application.DTOs;

public record RegisterDTO(string Login, string Password, Role Role = Role.User);
public record LoginDTO(string Login, string Password);
public record AuthResponseDTO(string Token);
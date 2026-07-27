using EventManagement.Bookings.Domain.Enums;

namespace EventManagement.Bookings.Application.DTOs;

public record RegisterDTO(string Login, string Password, Role Role = Role.User);
public record LoginDTO(string Login, string Password);
public record AuthResponseDTO(string Token);
public record UserResponseDto(Guid Id, string Login, Role Role);
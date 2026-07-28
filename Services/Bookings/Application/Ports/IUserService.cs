using EventManagement.Bookings.Application.DTOs;

namespace EventManagement.Bookings.Application.Ports;

public interface IUserService
{
    Task<UserResponseDto?> GetUserByIdAsync(Guid userId);
}
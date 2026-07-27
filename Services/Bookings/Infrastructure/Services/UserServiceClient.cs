using System.Text.Json;
using EventManagement.Bookings.Application.DTOs;
using EventManagement.Bookings.Application.Ports;
using Microsoft.Extensions.Logging;

namespace EventManagement.Bookings.Infrastructure.Services;

public class UserServiceClient : IUserService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UserServiceClient> _logger;

    public UserServiceClient(HttpClient httpClient, ILogger<UserServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<UserResponseDto?> GetUserByIdAsync(Guid userId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/users/{userId}");
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get user {UserId}. Status: {StatusCode}", userId, response.StatusCode);
                return null;
            }
            
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<UserResponseDto>(content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user {UserId}", userId);
            return null;
        }
    }
}
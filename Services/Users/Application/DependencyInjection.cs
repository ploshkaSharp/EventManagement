using Microsoft.Extensions.DependencyInjection;
using EventManagement.Users.Application.Services;

namespace EventManagement.Users.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        return services;
    }
}
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using EventManagement.Bookings.Application.Ports;
using EventManagement.Bookings.Infrastructure.Data;
using EventManagement.Bookings.Infrastructure.Repositories;
using EventManagement.Bookings.Infrastructure.Security;

namespace EventManagement.Bookings.Infrastructure;

/// <summary>
/// Extension-метод вызова в DI
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Регитсрация DbContext
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
        
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        
        services.Configure<JwtSettings>(
            configuration.GetSection("JwtSettings"));
        
        return services;
    }
}
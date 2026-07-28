using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using EventManagement.Users.Application.Ports;
using EventManagement.Users.Infrastructure.Data;
using EventManagement.Users.Infrastructure.Repositories;
using EventManagement.Users.Infrastructure.Security;

namespace EventManagement.Users.Infrastructure;

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

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        
        services.Configure<JwtSettings>(
            configuration.GetSection("JwtSettings"));
        
        return services;
    }
}
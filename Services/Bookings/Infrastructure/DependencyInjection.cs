using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using EventManagement.Bookings.Application.Ports;
using EventManagement.Bookings.Infrastructure.Data;
using EventManagement.Bookings.Infrastructure.Repositories;
using EventManagement.Bookings.Application.Services;
using EventManagement.Bookings.Infrastructure.Messaging;
using EventManagement.Bookings.Infrastructure.Services;

namespace EventManagement.Bookings.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IUserService, UserServiceClient>();        
        services.AddSingleton<IEventPublisher, KafkaEventPublisher>();

        // HTTP Client for User Service
        /*
        services.AddHttpClient<IUserService, UserServiceClient>(client =>
        {
            var usersServiceUrl = configuration["Services:Users:Url"] 
                ?? throw new InvalidOperationException("Services:Users:Url not configured");
            client.BaseAddress = new Uri(usersServiceUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        */

        services.AddScoped<IBookingService, BookingService>();

        // Producer
        services.AddSingleton<KafkaEventPublisher>();

        return services;
    }
}
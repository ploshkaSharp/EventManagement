using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using EventManagement.Bookings.Application.Ports;
using EventManagement.Bookings.Infrastructure.Data;
using EventManagement.Bookings.Infrastructure.Repositories;
using EventManagement.Bookings.Application.Services;
using EventManagement.Bookings.Infrastructure.Messaging;

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
        services.AddSingleton<IEventPublisher, KafkaEventPublisher>();
        services.AddScoped<IBookingService, BookingService>();
        // Producer
        //services.AddSingleton<KafkaEventPublisher>();
        services.AddHostedService<BookingProcessedConsumerService>();

        return services;
    }
}
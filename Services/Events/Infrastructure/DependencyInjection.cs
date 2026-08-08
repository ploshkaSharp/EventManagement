using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using EventManagement.Events.Application.Ports;
using EventManagement.Events.Application.Handlers;
using EventManagement.Events.Infrastructure.Data;
using EventManagement.Events.Infrastructure.Repositories;
using EventManagement.Events.Infrastructure.Messaging;
using EventManagement.Events.Infrastructure.Kafka;


namespace EventManagement.Events.Infrastructure;

/// <summary>
/// Extension-метод вызова в DI
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IBookingRequestedHandler, BookingRequestedHandler>();        
        services.AddScoped<IProcessedBookingRepository, ProcessedBookingRepository>();
        services.AddHostedService<BookingRequestedConsumerService>();
        services.AddHostedService<BookingCancelledConsumerService>();
        services.AddHostedService<TopicInitializer>();
        services.AddSingleton<IEventPublisher, KafkaEventPublisher>();

        return services;
    }
}
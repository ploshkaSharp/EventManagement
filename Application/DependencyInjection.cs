using Microsoft.Extensions.DependencyInjection;
using EventManagement.Application.Services;
using EventManagement.Application.Background;

namespace EventManagement.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddHostedService<BookingBackgroundService>();
        
        return services;
    }
}
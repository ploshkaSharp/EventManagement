using Microsoft.Extensions.DependencyInjection;
using EventManagement.Events.Application.Handlers;
using EventManagement.Events.Application.Services;

namespace EventManagement.Events.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IBookingRequestedHandler, BookingRequestedHandler>();

        return services;
    }
}
using Microsoft.Extensions.DependencyInjection;
using EventManagement.Bookings.Application.Services;

namespace EventManagement.Bookings.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IBookingService, BookingService>();

        return services;
    }
}
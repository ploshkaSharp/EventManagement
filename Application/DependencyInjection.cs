using Microsoft.Extensions.DependencyInjection;
using EventManagement.Application.Services;
using EventManagement.Application.Background;

namespace EventManagement.Application;

/// <summary>
/// Extension-метод вызова в DI
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Регистрация сервисов
    /// </summary>
    /// <param name="services">Сервисы</param>
    /// <returns></returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddHostedService<BookingBackgroundService>();
        
        return services;
    }
}
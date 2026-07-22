using Bookings.Application.Interfaces;
using Bookings.Application.Services;

namespace Bookings.Application;

/// <summary>
/// Методы регистрации зависимостей слоя Application сервиса бронирований.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Добавляет бизнес-сервисы бронирований в контейнер зависимостей.
    /// </summary>
    /// <param name="services">Коллекция сервисов приложения.</param>
    /// <returns>Коллекция сервисов для дальнейшей настройки.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IBookingService, BookingService>();

        return services;
    }
}
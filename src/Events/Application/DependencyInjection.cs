using Events.Application.Interfaces;
using Events.Application.Services;

namespace Events.Application;

/// <summary>
/// Методы регистрации зависимостей слоя Application сервиса событий.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Добавляет бизнес-сервисы событий в контейнер зависимостей.
    /// </summary>
    /// <param name="services">Коллекция сервисов приложения.</param>
    /// <returns>Коллекция сервисов для дальнейшей настройки.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IEventService, EventService>();

        return services;
    }
}
using Users.Application.Interfaces;
using Users.Application.Services;

namespace Users.Application;

/// <summary>
/// Методы регистрации зависимостей слоя Application сервиса пользователей.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Добавляет бизнес-сервисы пользователей в контейнер зависимостей.
    /// </summary>
    /// <param name="services">Коллекция сервисов приложения.</param>
    /// <returns>Коллекция сервисов для дальнейшей настройки.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();

        return services;
    }
}
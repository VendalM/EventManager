using EventManager.Application.Interfaces;
using EventManager.Application.Repositories;
using EventManager.Application.Services;
using EventManager.Mappers;

namespace EventManager.Application
{
    /// <summary>
    /// Класс для регистрации зависимостей слоя приложения в DI-контейнере
    /// </summary>
    public static class DependencyInjection
    {
        /// <summary>
        /// Добавляет сервисы слоя приложения в DI-контейнер
        /// </summary>
        /// <param name="services">Коллекция сервисов</param>
        /// <returns>Коллекция сервисов для дальнейшей конфигурации</returns>
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            // Репозиторий
            services.AddScoped<IBookingRepository, BookingRepository>();
            
            // Бизнес-логика
            services.AddScoped<IEventService, EventService>();
            services.AddScoped<IBookingService, BookingService>();
            
            // Регистрация AutoMapper
            services.AddAutoMapper(typeof(EventMappingProfile).Assembly);

            return services;
        }
    }
}
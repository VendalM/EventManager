using AutoMapper;
using AutoMapper.Configuration;
using Events.Application.Interfaces;
using Events.Application.Services;
using Events.Infrastructure.DataAccess;
using Events.Infrastructure.Mappers;
using Events.Infrastructure.Messaging;
using Events.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Events.Infrastructure;

/// <summary>
/// Методы регистрации инфраструктурных зависимостей сервиса событий.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Добавляет контекст БД, репозитории, маппинг и Kafka-компоненты сервиса событий.
    /// </summary>
    /// <param name="services">Коллекция сервисов приложения.</param>
    /// <param name="configuration">Конфигурация приложения.</param>
    /// <returns>Коллекция сервисов для дальнейшей настройки.</returns>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // DbContext
        var connectionString = configuration.GetConnectionString("DefaultConnection")
                               ?? configuration.GetConnectionString("Default")
                               ?? throw new InvalidOperationException("Connection string is not configured.");

        services.AddDbContext<EventsAppDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Репозитории
        services.AddScoped<IEventRepository, EventRepository>();

        // AutoMapper (профили, лежащие в Infrastructure)
        services.AddAutoMapper(typeof(EventMappingProfile).Assembly);
        
        // Обмен сообщениями
        services.AddHostedService<KafkaTopicInitializer>();
        services.AddScoped<IBookingEventHandler, BookingEventHandler>();
        services.AddHostedService<KafkaBookingEventsConsumer>();
        services.Configure<KafkaOptions>(configuration.GetSection("Kafka"));
        services.AddSingleton<IEventsEventPublisher, KafkaBookingEventPublisher>();

        return services;
    }
}
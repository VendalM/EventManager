using Bookings.Application.Interfaces;
using Bookings.Application.Services;
using Bookings.Infrastructure.DataAccess;
using Bookings.Infrastructure.Mappers;
using Bookings.Infrastructure.Messaging;
using Bookings.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Bookings.Infrastructure;

/// <summary>
/// Методы регистрации инфраструктурных зависимостей сервиса бронирований.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Добавляет контекст БД, репозитории, маппинг и Kafka-компоненты сервиса бронирований.
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

        services.AddDbContext<BookingsAppDbContext>(options =>
            options.UseNpgsql(connectionString));

        // AutoMapper (профили, лежащие в Infrastructure)
        services.AddAutoMapper(typeof(BookingsMappingProfile).Assembly);

        // Репозитории
        services.AddScoped<IBookingRepository, BookingRepository>();
        
        // Обмен сообщениями
        services.AddScoped<IBookingStatusHandler, BookingStatusHandler>();
        services.AddHostedService<KafkaBookingEventsConsumer>();
        services.Configure<KafkaOptions>(configuration.GetSection("Kafka"));
        services.AddSingleton<IBookingEventPublisher, KafkaBookingEventPublisher>();

        return services;
    }
}
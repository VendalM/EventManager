using Bookings.Application.Interfaces;
using Bookings.Application.Services;
using Bookings.Infrastructure.DataAccess;
using Bookings.Infrastructure.Messaging;
using Bookings.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Bookings.Infrastructure;

public static class DependencyInjection
{
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
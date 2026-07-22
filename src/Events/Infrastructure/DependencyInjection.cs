using AutoMapper;
using AutoMapper.Configuration;
using Events.Application.Interfaces;
using Events.Application.Services;
using Events.Infrastructure.DataAccess;
using Events.Infrastructure.Mappers;
using Events.Infrastructure.Messaging;
using Events.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Events.Infrastructure;

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

        services.AddDbContext<EventsAppDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Репозитории
        services.AddScoped<IEventRepository, EventRepository>();

        // AutoMapper (профили, лежащие в Infrastructure)
        services.AddAutoMapper(typeof(EventMappingProfile).Assembly);
        
        // Обмен сообщениями
        services.AddScoped<IBookingEventHandler, BookingEventHandler>();
        services.AddHostedService<KafkaBookingEventsConsumer>();
        services.Configure<KafkaOptions>(configuration.GetSection("Kafka"));
        services.AddSingleton<IEventsEventPublisher, KafkaBookingEventPublisher>();

        return services;
    }
}
using Application.Interfaces;
using Application.Services;
using EventManager.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IBookingService, BookingService>();
        
        return services;
    }
}
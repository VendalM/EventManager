using EventManager.Application.Interfaces;
using EventManager.Application.Services;

namespace EventManager.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            // Бизнес-логика
            services.AddScoped<IEventService, EventService>();

            return services;
        }
    }
}
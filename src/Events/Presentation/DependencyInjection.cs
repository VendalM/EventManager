using Events.Presentation.Filters;

namespace Events.Presentation
{
    /// <summary>
    /// Класс для регистрации зависимостей слоя представления в DI-контейнере
    /// </summary>
    public static class DependencyInjection
    {
        /// <summary>
        /// Добавляет сервисы слоя представления в DI-контейнер
        /// </summary>
        /// <param name="services">Коллекция сервисов</param>
        /// <returns>Коллекция сервисов для дальнейшей конфигурации</returns>
        /// <remarks>
        /// Регистрирует:
        /// <list type="bullet">
        /// <item>Контроллеры</item>
        /// <item>API Explorer для конечных точек</item>
        /// <item>Swagger с поддержкой XML-комментариев</item>
        /// </list>
        /// </remarks>
        public static IServiceCollection AddPresentation(this IServiceCollection services)
        {
            services.AddControllers(options =>
            {
                options.Filters.Add<ValidationExceptionFilter>();
            });
            
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();

            return services;
        }
    }
}

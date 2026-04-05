namespace EventManager.Middlewares;

/// <summary>
/// Класс для регистрации middleware
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Добавляет глобальную обработку исключений в pipeline
    /// </summary>
    public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder app)
    {
        return app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
    }
}

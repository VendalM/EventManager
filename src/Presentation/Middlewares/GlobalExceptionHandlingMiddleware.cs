using Application.Exceptions;
using Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Middlewares;

/// <summary>
/// Перехватчик ошибок
/// </summary>
public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    /// <summary>
    ///  Создание экземпляра класса <see cref="GlobalExceptionHandlingMiddleware"/>.
    /// </summary>
    /// <param name="next">Следующий middleware в конвейере</param>
    /// <param name="logger">Логгер для записи ошибок</param>
    public GlobalExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    ///  Обрабатывает HTTP-запрос
    /// </summary>
    /// <param name="httpContext">Запрос</param>
    public async Task InvokeAsync(HttpContext httpContext)
    {
        try
        {
            await _next(httpContext);
        }
        catch (Exception ex)
        {
            await HandleException(httpContext, ex);
        }
    }

    /// <summary>
    ///  Обрабатывает HTTP-запрос, перехватывая необработанные исключения и логируя их
    /// </summary>
    /// <param name="httpContext">Запрос</param>
    /// <param name="ex">Ошибка</param>
    private async Task HandleException(HttpContext httpContext, Exception ex)
    {
        _logger.LogError(
            ex,
            "Unhandled exception. Method={Method}, Path={Path}, RequestId={RequestId}",
            httpContext.Request.Method,
            httpContext.Request.Path,
            httpContext.Request.Headers["x-request-id"]);

        if (httpContext.Response.HasStarted)
        {
            return;
        }

        var statusCode = MapStatusCode(ex);

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/json";

        var error = new ProblemDetails
        {
            Status = statusCode,
            Detail = ex.Message
        };

        await httpContext.Response.WriteAsJsonAsync(error);
    }

    /// <summary>
    /// Преобразует тип исключения в соответствующий HTTP статус код
    /// </summary>
    /// <param name="ex">Ошибка</param>
    private static int MapStatusCode(Exception ex)
        => ex switch
        {
            ValidationException ve => StatusCodes.Status400BadRequest,
            NotFoundException nfe => StatusCodes.Status404NotFound,
            NoAvailableSeatsException nfe => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };
}
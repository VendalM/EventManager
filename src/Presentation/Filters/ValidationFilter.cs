using Application.Exceptions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Presentation.Filters;

/// <summary>
/// Фильтр для валидации, который преобразует ошибки валидации в ValidationException
/// </summary>
/// <remarks>
/// Перехватывает ошибки ModelState, возникающие при валидации DTO,
/// и преобразует их в исключение ValidationException для централизованной обработки
/// </remarks>
public class ValidationExceptionFilter : IActionFilter
{
    /// <summary>
    /// Выполняется перед выполнением действия контроллера
    /// </summary>
    /// <param name="context">Контекст выполнения действия</param>
    /// <remarks>
    /// Проверяет состояние модели (ModelState):
    /// <list type="bullet">
    /// <item>Если ModelState валиден - выполнение продолжается</item>
    /// <item>Если ModelState невалиден - извлекает первое сообщение об ошибке</item>
    /// <item>Выбрасывает ValidationException с текстом ошибки</item>
    /// </list>
    /// </remarks>
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid)
        {
            // Извлекаем первое сообщение об ошибке из ModelState
            var firstError = context.ModelState
                .SelectMany(kvp => kvp.Value?.Errors ?? Enumerable.Empty<ModelError>())
                .FirstOrDefault();
            
            var errorMessage = firstError?.ErrorMessage ?? "Ошибка валидации";
       
            throw new ValidationException(errorMessage);
        }
    }

    /// <summary>
    /// Выполняется после выполнения действия контроллера
    /// </summary>
    /// <param name="context">Контекст выполнения действия</param>
    /// <remarks>
    /// В текущей реализации не выполняет никаких действий,
    /// но оставлен для возможного расширения функциональности
    /// </remarks>
    public void OnActionExecuted(ActionExecutedContext context)
    {
        // Метод не реализован, так как обработка ошибок происходит в OnActionExecuting
    }
}
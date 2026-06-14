namespace Application.Exceptions;

/// <summary>
/// Исключение, возникающее при ошибках валидации данных
/// </summary>
public class ValidationException : Exception
{
    /// <summary>
    /// Создает новый экземпляр исключения валидации
    /// </summary>
    public ValidationException()
    {
    }

    /// <summary>
    /// Создает новый экземпляр исключения валидации с указанным сообщением
    /// </summary>
    /// <param name="message">Сообщение об ошибке</param>
    public ValidationException(string message) 
        : base(message)
    {
    }

    /// <summary>
    /// Создает новый экземпляр исключения валидации с указанным сообщением и внутренним исключением
    /// </summary>
    /// <param name="message">Сообщение об ошибке</param>
    /// <param name="innerException">Внутреннее исключение</param>
    public ValidationException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
}
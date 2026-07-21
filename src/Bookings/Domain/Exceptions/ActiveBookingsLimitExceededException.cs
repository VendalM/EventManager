namespace Bookings.Domain.Exceptions;

/// <summary>
/// Исключение, возникающее при превышении лимита активных броней пользователя.
/// </summary>
public class ActiveBookingsLimitExceededException : Exception
{
    /// <summary>
    /// Сообщение об ошибке при превышении лимита активных броней пользователя.
    /// </summary>
    public ActiveBookingsLimitExceededException(int limit)
        : base($"Превышен лимит активных броней. Максимум: {limit}.")
    {
    }
}
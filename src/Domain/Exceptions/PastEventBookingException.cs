namespace Domain.Exceptions;

/// <summary>
/// Исключение, возникающее при попытке забронировать прошедшее событие.
/// </summary>
public class PastEventBookingException : Exception
{
    /// <summary>
    /// Сообщение об ошибке при попытке забронировать прошедшее событие.
    /// </summary>
    public PastEventBookingException(string eventTitle)
        : base($"Нельзя забронировать прошедшее событие '{eventTitle}'.")
    {
    }
}
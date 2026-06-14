namespace Domain.Exceptions;

/// <summary>
/// Класс для обработки ошибок, связанных с отсутствием доступных мест на событие
/// </summary>
public class NoAvailableSeatsException : Exception
{
    /// <summary>
    /// Сообщение об ошибке, указывающее на то, что нет свободных мест на событие с указанным названием
    /// </summary>
    public NoAvailableSeatsException(string title) : base($"Нет свободных мест на событие '{title}'.")
    {
    }
}
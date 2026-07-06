namespace Domain.Exceptions;

/// <summary>
/// Исключение, возникающее при попытке установить общее количество мест меньше, чем уже занято.
/// </summary>
public class TotalSeatsTooLowException : Exception
{
    /// <summary>
    /// Сообщение об ошибке с указанием нового общего количества мест и количества уже занятых мест.
    /// </summary>
    public TotalSeatsTooLowException(string eventTitle, int newTotalSeats, int occupiedSeats) 
        : base($"Для события '{eventTitle}' невозможно установить общее количество мест {newTotalSeats}, так как уже занято {occupiedSeats} мест.")
    {
    }
}
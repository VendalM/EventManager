namespace Application.Models;

/// <summary>
/// Базовый класс события
/// </summary>
public class EventDto
{
    /// <summary>
    /// Идентификатор
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// Заголовок события
    /// </summary>
    public string Title { get; set; }
    
    /// <summary>
    /// Описание события
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Время начала события
    /// </summary>
    public DateTime StartDate { get; set; }
    
    /// <summary>
    /// Время конца события
    /// </summary>
    public DateTime EndDate { get; set; }
    
    /// <summary>
    /// Общее количество мест на событии
    /// </summary>
    public int TotalSeats { get; set; }
    
    /// <summary>
    /// Текущее количество свободных мест
    /// </summary>
    public int AvailableSeats { get; set; }

    /// <summary>
    /// Проверяет, можно ли зарезервировать указанное количество мест на событии
    /// </summary>
    /// @param count Количество мест для резервирования (по умолчанию 1)
    public bool TryReserveSeats(int count = 1)
    {
        if (AvailableSeats < count)
            return false;
    
        AvailableSeats -= count;
        return true;
    }
    
    /// <summary>
    /// Снимает резервирование мест на событии, если это возможно
    /// </summary>
    /// @param count Количество мест для снятия резервирования (по умолчанию 1)
    public void ReleaseSeats(int count = 1)
    {
        AvailableSeats += count;
        if (AvailableSeats > TotalSeats)
            AvailableSeats = TotalSeats;
    }
}
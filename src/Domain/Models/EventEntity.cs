namespace Domain.Models;

/// <summary>
/// Сущность события для хранения в БД
/// </summary>
public class EventEntity
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
    /// Список бронирований, связанных с этим событием
    /// </summary>
    public List<BookingEntity> Bookings { get; set; }
}

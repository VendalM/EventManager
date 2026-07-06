using Domain.Enums;

namespace Domain.Models;

/// <summary>
/// Сущность бронирования для хранения в БД
/// </summary>
public class BookingEntity
{
    /// <summary>
    /// Yникальный идентификатор брони
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// Идентификатор события, к которому относится бронь
    /// </summary>
    public Guid EventId { get; set; }
    
    /// <summary>
    /// Идентификатор пользователя, который создал бронь
    /// </summary>
    public Guid UserId { get; set; }
    
    /// <summary>
    /// Текущий статус брони
    /// </summary>
    public BookingStatus Status { get; set; }
    
    /// <summary>
    /// Дата и время создания брони
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Дата и время обработки брони
    /// </summary>
    public DateTime? ProcessedAt { get; set; }
    
    /// <summary>
    /// Событие, к которому относится бронь
    /// </summary>
    public EventEntity Event { get; set; }
    
    /// <summary>
    /// Пользователь, создавший бронь
    /// </summary>
    public UserEntity User { get; set; }
}
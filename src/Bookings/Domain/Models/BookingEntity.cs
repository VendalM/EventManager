using Contracts.Bookings;

namespace Bookings.Domain.Models;

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
    /// Подтверждение брони, которое устанавливает статус в "Подтверждено" и сохраняет дату обработки
    /// </summary>
    public void Confirm()
    {
        Status = BookingStatus.Confirmed;
        ProcessedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Отклонение брони, которое устанавливает статус в "Отклонено" и сохраняет дату обработки
    /// </summary>
    public void Reject()
    {
        Status = BookingStatus.Rejected;
        ProcessedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Отмена брони, которое устанавливает статус в "Отменено" и сохраняет дату обработки
    /// </summary>
    public void Cancel()
    {
        if (Status == BookingStatus.Cancelled)
        {
            return;
        }

        Status = BookingStatus.Cancelled;
        ProcessedAt = DateTime.UtcNow;
    }
}
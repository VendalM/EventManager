using Domain.Enums;

namespace Application.Models;

/// <summary>
/// Базовый класс бронирования
/// </summary>
public class BookingDto
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
}
namespace EventManager.Models;

/// <summary>
/// Сущность события для хранения в БД
/// </summary>
public class EventEntity
{
    /// <summary>
    /// Идентификатор
    /// </summary>
    public int Id { get; set; }
    
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
}

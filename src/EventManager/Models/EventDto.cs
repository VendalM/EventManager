namespace EventManager.Models;

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
}
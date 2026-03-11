using System.ComponentModel.DataAnnotations;

namespace EventManager.Models;

/// <summary>
/// Базовый класс события
/// </summary>
public class EventDto
{
    /// <summary>
    /// Идентификатор
    /// </summary>
    [Required]
    public int Id { get; set; }
    
    /// <summary>
    /// Заголовок события
    /// </summary>
    [Required]
    public string Title { get; set; }
    
    /// <summary>
    /// Описание события
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Время начала события
    /// </summary>
    [Required]
    public DateTime StartDate { get; set; }
    
    /// <summary>
    /// Время конца события
    /// </summary>
    [Required]
    public DateTime EndDate { get; set; }
}
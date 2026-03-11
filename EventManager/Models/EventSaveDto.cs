using System.ComponentModel.DataAnnotations;

namespace EventManager.Models;

/// <summary>
/// Модель сохранения события
/// </summary>
public class EventSaveDto
{
    /// <summary>
    /// Заголовок события
    /// </summary>
    [Required(AllowEmptyStrings = false, ErrorMessage = "Не заполнен заголовок события")]
    public string Title { get; set; }
    
    /// <summary>
    /// Описание события
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Время начала события
    /// </summary>
    [Required(ErrorMessage = "Не указано время начала события")]
    public DateTime StartDate { get; set; }
    
    /// <summary>
    /// Время конца события
    /// </summary>
    [Required(ErrorMessage = "Не указано время окончания события")]
    public DateTime EndDate { get; set; }
}
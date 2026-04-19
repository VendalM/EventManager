using System.ComponentModel.DataAnnotations;

namespace EventManager.Models;

/// <summary>
/// Модель сохранения события
/// </summary>
public class EventSaveDto : IValidatableObject 
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
    public DateTime? StartDate { get; set; }
    
    /// <summary>
    /// Время конца события
    /// </summary>
    public DateTime? EndDate { get; set; }
    
    /// <summary>
    /// Общее количество мест на событии
    /// </summary>
    public int? TotalSeats { get; set; }
    
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StartDate == default)
            yield return new ValidationResult("Не указано время начала события", new[] { nameof(StartDate) });
    
        if (EndDate == default)
            yield return new ValidationResult("Не указано время окончания события", new[] { nameof(EndDate) });
        
        if (TotalSeats == default)
            yield return new ValidationResult("Не указано общее количество мест на событии", new[] { nameof(TotalSeats) });
    
        if (StartDate == default || EndDate == default)
            yield break;
    
        if (StartDate >= EndDate)
            yield return new ValidationResult("Дата окончания события должна быть больше даты начала.", new[] { nameof(EndDate) });
        
        if (TotalSeats <= 0)
            yield return new ValidationResult("Общее количество мест на событии не может быть отрицательным", new[] { nameof(TotalSeats) });
    }
}
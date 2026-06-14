namespace Application.Models;

/// <summary>
/// Модель для представления результатов с пагинацией
/// </summary>
/// <typeparam name="T">Тип элементов в результатах</typeparam>
public class PaginatedResult<T>
{
    /// <summary>
    /// Список элементов на текущей странице
    /// </summary>
    public required List<T> Items { get; set; }
    
    /// <summary>
    /// Общее количество элементов, соответствующих запросу (без учета пагинации)
    /// </summary>
    public int TotalItems { get; set; }
    
    /// <summary>
    /// Номер текущей страницы (начинается с 1)
    /// </summary>
     public int Page { get; set; }
     
    /// <summary>
    /// Количество элементов на странице
    /// </summary>
     public int PageSize { get; set; }
}
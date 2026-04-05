namespace EventManager.Exceptions;

/// <summary>
/// Класс для обработки ошибок, связанных с отсутствием сущности в базе данных
/// </summary>
public class NotFoundException : Exception
{
    /// <summary>
    /// Сообщение об ошибке, указывающее на то, что сущность с указанным идентификатором не найдена
    /// </summary>
    public NotFoundException(int id) : base($"Сущность с идентификатором '{id}' не найдена.")
    {
    }
    
    /// <summary>
    /// Сообщение об ошибке, указывающее на то, что сущность с указанным идентификатором не найдена
    /// </summary>
    public NotFoundException(Guid id) : base($"Сущность с идентификатором '{id}' не найдена.")
    {
    }
}
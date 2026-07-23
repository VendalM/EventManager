namespace Users.Application.Exceptions;

/// <summary>
/// Исключение, возникающее при отсутствии прав на выполнение операции.
/// </summary>
public class OperationForbiddenException : Exception
{
    /// <summary>
    /// Сообщение об ошибке при отсутствии прав на выполнение операции.
    /// </summary>
    public OperationForbiddenException()
        : base("Недостаточно прав для выполнения операции.")
    {
    }
}
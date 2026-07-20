namespace Application.Interfaces;

/// <summary>
/// Компонент для хеширования паролей и проверки соответствия пароля хешу.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Создает хеш для указанного пароля.
    /// </summary>
    /// <param name="password">Пароль.</param>
    /// <returns>Хеш пароля.</returns>
    string Hash(string password);

    /// <summary>
    /// Проверяет соответствие пароля указанному хешу.
    /// </summary>
    /// <param name="password">Пароль.</param>
    /// <param name="passwordHash">Хеш пароля.</param>
    /// <returns>Признак соответствия пароля хешу.</returns>
    bool Verify(string password, string passwordHash);
}
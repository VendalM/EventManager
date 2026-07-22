namespace Users.Application.Models;

/// <summary>
/// Модель пары токенов, выдаваемой после входа или обновления сессии.
/// </summary>
public class AuthTokenDto
{
    /// <summary>
    /// JWT access-токен.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// JWT access-токен.
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Refresh-токен для перевыпуска access-токена.
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;
}
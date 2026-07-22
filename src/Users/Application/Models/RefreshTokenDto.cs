namespace Users.Application.Models;

/// <summary>
/// Модель запроса с refresh-токеном.
/// </summary>
public class RefreshTokenDto
{
    /// <summary>
    /// Refresh-токен.
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;
}
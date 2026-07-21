namespace Users.Domain.Models;

/// <summary>
/// Refresh-токен пользователя.
/// </summary>
public class RefreshTokenEntity
{
    /// <summary>
    /// Идентификатор refresh-токена.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Идентификатор пользователя.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// SHA-256 хеш refresh-токена.
    /// </summary>
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>
    /// Дата создания.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Дата окончания действия.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Дата отзыва токена.
    /// </summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>
    /// Пользователь, которому принадлежит refresh-токен.
    /// </summary>
    public UserEntity User { get; set; } = null!;
}
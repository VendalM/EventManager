using Users.Domain.Models;

namespace Users.Application.Interfaces;

/// <summary>
/// Репозиторий refresh-токенов.
/// </summary>
public interface IRefreshTokenRepository
{
    /// <summary>
    /// Сохранить refresh-токен.
    /// </summary>
    Task<RefreshTokenEntity> CreateAsync(RefreshTokenEntity refreshToken);

    /// <summary>
    /// Найти refresh-токен по хешу.
    /// </summary>
    Task<RefreshTokenEntity?> GetByTokenHashAsync(string tokenHash);

    /// <summary>
    /// Отозвать refresh-токен.
    /// </summary>
    Task RevokeAsync(RefreshTokenEntity refreshToken);
}
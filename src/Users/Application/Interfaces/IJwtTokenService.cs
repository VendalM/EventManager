using Users.Domain.Models;

namespace Users.Application.Interfaces;

/// <summary>
/// Контракт сервиса генерации JWT и расчета срока жизни refresh-токена.
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Создать JWT-токен для пользователя.
    /// </summary>
    string Generate(UserEntity user);

    /// <summary>
    /// Рассчитать дату истечения refresh-токена.
    /// </summary>
    DateTime GetRefreshTokenExpiresAt(DateTime createdAt);
}
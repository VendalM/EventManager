using Domain.Models;

namespace Application.Interfaces;

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
using Application.Models;
using Domain.Models;

namespace Application.Interfaces;

/// <summary>
/// Контракт сервиса пользователей.
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Получить пользователя по идентификатору.
    /// </summary>
    Task<UserEntity?> GetByIdAsync(Guid id);

    /// <summary>
    /// Зарегистрировать пользователя.
    /// </summary>
    Task<UserDto> RegisterAsync(UserRegistrationDto user);

    /// <summary>
    /// Выполнить вход пользователя.
    /// </summary>
    Task<AuthTokenDto> LoginAsync(UserLoginDto user);

    /// <summary>
    /// Перевыпустить пару access/refresh-токенов.
    /// </summary>
    Task<AuthTokenDto> RefreshAsync(RefreshTokenDto refreshToken);

    /// <summary>
    /// Выполнить выход пользователя, отозвав refresh-токен.
    /// </summary>
    Task LogoutAsync(RefreshTokenDto refreshToken);
}
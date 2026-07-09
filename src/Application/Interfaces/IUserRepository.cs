using Domain.Models;

namespace Application.Interfaces;

/// <summary>
/// Контракт для репозитория, который отвечает за управление данными пользователей
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Получение пользователя по идентификатору
    /// </summary>
    /// <param name="id">Идентификатор брони</param>
    Task<UserEntity?> GetByIdAsync(Guid id);

    /// <summary>
    /// Получение пользователя по логину.
    /// </summary>
    /// <param name="login">Логин.</param>
    Task<UserEntity?> GetByLoginAsync(string login);

    /// <summary>
    /// Создать пользователь
    /// </summary>
    /// <param name="user">Пользователь</param>
    Task<UserEntity> Create(UserEntity user);
}
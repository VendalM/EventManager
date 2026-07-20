using Domain.Enums;

namespace Domain.Models;

/// <summary>
/// Сущность пользователя для хранения в БД
/// </summary>
public class UserEntity
{
    /// <summary>
    /// Уникальный идентификатор пользователя
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// Логин
    /// </summary>
    public string Login { get; set; }
    
    /// <summary>
    /// Хэш пароля
    /// </summary>
    public string PasswordHash { get; set; }
    
    /// <summary>
    /// Роль
    /// </summary>
    public Roles Role { get; set; }
}
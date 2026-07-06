using Application.Enums;

namespace Application.Models;

public class UserDto
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
    public string Password { get; set; }
    
    /// <summary>
    /// Роль
    /// </summary>
    public Roles Role { get; set; }
}
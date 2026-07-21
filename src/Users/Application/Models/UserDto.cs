using Contracts.Auth;

namespace Users.Application.Models;

public class UserDto
{
    /// <summary>
    /// Уникальный идентификатор пользователя.
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// Логин.
    /// </summary>
    public string Login { get; set; } = string.Empty;

    /// <summary>
    /// Роль.
    /// </summary>
    public Roles Role { get; set; }
}
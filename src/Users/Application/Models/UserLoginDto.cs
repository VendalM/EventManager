namespace Users.Application.Models;

/// <summary>
/// Dto для входа пользователя
/// </summary>
public class UserLoginDto
{
    /// <summary>
    /// Логин.
    /// </summary>
    public string Login { get; set; } = string.Empty;

    /// <summary>
    /// Пароль.
    /// </summary>
    public string Password { get; set; } = string.Empty;
}
namespace Contracts.Auth;

/// <summary>
/// DTO для регистрации пользователя.
/// </summary>
public class UserRegistrationDto
{
    /// <summary>
    /// Логин.
    /// </summary>
    public string Login { get; set; } = string.Empty;

    /// <summary>
    /// Пароль.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Роль пользователя. По умолчанию User.
    /// </summary>
    public Roles Role { get; set; } = Roles.User;
}

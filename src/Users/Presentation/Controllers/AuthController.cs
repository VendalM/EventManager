using Contracts.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Users.Application.Interfaces;
using Users.Application.Models;

namespace Users.Presentation.Controllers;

/// <summary>
/// Контроллер для регистрации, входа, обновления токенов и выхода пользователей.
/// </summary>
[ApiController]
[Route("/auth")]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;

    /// <summary>
    /// Создать контроллер аутентификации.
    /// </summary>
    public AuthController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// Зарегистрировать пользователя.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<UserDto>> Register([FromBody] UserRegistrationDto user)
    {
        await _userService.RegisterAsync(user);
        return NoContent();
    }

    /// <summary>
    /// Выполнить вход пользователя и получить пару access/refresh-токенов.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthTokenDto>> Login([FromBody] UserLoginDto user)
    {
        var result = await _userService.LoginAsync(user);
        return Ok(result);
    }

    /// <summary>
    /// Обновить пару access/refresh-токенов.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthTokenDto>> Refresh([FromBody] RefreshTokenDto refreshToken)
    {
        var result = await _userService.RefreshAsync(refreshToken);
        return Ok(result);
    }

    /// <summary>
    /// Выполнить выход пользователя, отозвав refresh-токен.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenDto refreshToken)
    {
        await _userService.LogoutAsync(refreshToken);
        return NoContent();
    }
}

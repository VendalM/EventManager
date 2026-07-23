using System.Security.Claims;

namespace Bookings.Presentation.Controllers;

/// <summary>
/// Вспомогательные методы для чтения claims текущего пользователя.
/// </summary>
public static class ControllerClaimsExtensions
{
    /// <summary>
    /// Получить идентификатор текущего пользователя из claims.
    /// </summary>
    public static Guid GetCurrentUserId(this ClaimsPrincipal user)
    {
        var userId = user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userId, out var parsedUserId))
        {
            throw new UnauthorizedAccessException("Не удалось определить текущего пользователя.");
        }

        return parsedUserId;
    }
}
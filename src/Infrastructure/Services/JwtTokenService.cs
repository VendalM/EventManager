using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Application.Interfaces;
using Domain.Models;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Services;

/// <summary>
/// Сервис генерации JWT-токенов для пользователей.
/// </summary>
public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Создает экземпляр сервиса генерации JWT-токенов.
    /// </summary>
    /// <param name="configuration">Конфигурация приложения с параметрами секции Jwt.</param>
    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Формирует подписанный JWT-токен по данным пользователя.
    /// </summary>
    /// <param name="user">Пользователь, для которого выпускается токен.</param>
    /// <returns>JWT-токен со сроком действия из конфигурации.</returns>
    public string Generate(UserEntity user)
    {
        var secret = _configuration["Jwt:Secret"];
        var issuer = _configuration["Jwt:Issuer"];
        var audience = _configuration["Jwt:Audience"];
        var lifetimeMinutesText = _configuration["Jwt:LifetimeMinutes"];

        if (string.IsNullOrWhiteSpace(secret))
        {
            throw new InvalidOperationException("JWT secret is not configured.");
        }

        var lifetimeMinutes = int.TryParse(lifetimeMinutesText, out var parsedLifetime)
            ? parsedLifetime
            : 60;

        var now = DateTimeOffset.UtcNow;
        var header = new Dictionary<string, object>
        {
            ["alg"] = "HS256",
            ["typ"] = "JWT"
        };

        var payload = new Dictionary<string, object?>
        {
            ["sub"] = user.Id.ToString(),
            ["login"] = user.Login,
            ["role"] = user.Role.ToString(),
            ["jti"] = Guid.NewGuid().ToString(),
            ["iat"] = now.ToUnixTimeSeconds(),
            ["nbf"] = now.ToUnixTimeSeconds(),
            ["exp"] = now.AddMinutes(lifetimeMinutes).ToUnixTimeSeconds(),
            ["iss"] = issuer,
            ["aud"] = audience
        };

        var unsignedToken = $"{Encode(header)}.{Encode(payload)}";
        var signature = Sign(unsignedToken, secret);

        return $"{unsignedToken}.{signature}";
    }

    /// <inheritdoc />
    public DateTime GetRefreshTokenExpiresAt(DateTime createdAt)
    {
        var refreshLifetimeDaysText = _configuration["Jwt:RefreshLifetimeDays"];
        var refreshLifetimeDays = int.TryParse(refreshLifetimeDaysText, out var parsedLifetime)
            ? parsedLifetime
            : 7;

        return createdAt.AddDays(refreshLifetimeDays);
    }

    /// <summary>
    /// Сериализует часть JWT и кодирует ее в Base64Url.
    /// </summary>
    private static string Encode(object value)
    {
        var json = JsonSerializer.Serialize(value);
        return Base64UrlEncode(Encoding.UTF8.GetBytes(json));
    }

    /// <summary>
    /// Создает HMAC-SHA256 подпись для header и payload токена.
    /// </summary>
    private static string Sign(string data, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var signature = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Base64UrlEncode(signature);
    }

    /// <summary>
    /// Преобразует байты в формат Base64Url, используемый JWT.
    /// </summary>
    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
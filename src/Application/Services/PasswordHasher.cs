using System.Security.Cryptography;
using System.Text;
using Application.Interfaces;

namespace Application.Services;

/// <summary>
/// Сервис для хеширования паролей и проверки соответствия пароля хешу.
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    /// <inheritdoc />
    public string Hash(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes);
    }

    /// <inheritdoc />
    public bool Verify(string password, string passwordHash)
    {
        var hash = Hash(password);
        return hash == passwordHash;
    }
}
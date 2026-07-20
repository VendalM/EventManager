using Application.Interfaces;
using Domain.Models;
using Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// Репозиторий refresh-токенов.
/// </summary>
public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AppDbContext _context;

    /// <summary>
    /// Создать репозиторий refresh-токенов.
    /// </summary>
    public RefreshTokenRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<RefreshTokenEntity> CreateAsync(RefreshTokenEntity refreshToken)
    {
        refreshToken.CreatedAt = NormalizeToUtc(refreshToken.CreatedAt);
        refreshToken.ExpiresAt = NormalizeToUtc(refreshToken.ExpiresAt);
        refreshToken.RevokedAt = NormalizeToUtc(refreshToken.RevokedAt);

        var entry = await _context.RefreshTokens.AddAsync(refreshToken);
        await _context.SaveChangesAsync();

        return entry.Entity;
    }

    /// <inheritdoc />
    public Task<RefreshTokenEntity?> GetByTokenHashAsync(string tokenHash)
    {
        return _context.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash);
    }

    /// <inheritdoc />
    public async Task RevokeAsync(RefreshTokenEntity refreshToken)
    {
        refreshToken.RevokedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    private static DateTime NormalizeToUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }

    private static DateTime? NormalizeToUtc(DateTime? value)
    {
        return value.HasValue ? NormalizeToUtc(value.Value) : null;
    }
}
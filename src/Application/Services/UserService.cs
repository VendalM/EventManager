using System.Security.Cryptography;
using Application.Exceptions;
using Application.Interfaces;
using Application.Models;
using AutoMapper;
using Domain.Models;

namespace Application.Services;

/// <summary>
/// Сервис для работы с пользователями.
/// </summary>
public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IMapper _mapper;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    /// <summary>
    /// Создать сервис пользователей.
    /// </summary>
    public UserService(
        IMapper mapper,
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService)
    {
        _mapper = mapper;
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    /// <inheritdoc />
    public async Task<UserEntity?> GetByIdAsync(Guid id)
    {
        return await _userRepository.GetByIdAsync(id);
    }

    /// <inheritdoc />
    public async Task<UserDto> RegisterAsync(UserRegistrationDto user)
    {
        var existingUser = await _userRepository.GetByLoginAsync(user.Login);
        if (existingUser != null)
        {
            throw new ValidationException("Пользователь с таким логином уже существует.");
        }

        var newUser = _mapper.Map<UserEntity>(user);
        newUser.Id = Guid.NewGuid();
        newUser.Role = user.Role;
        newUser.PasswordHash = _passwordHasher.Hash(user.Password);

        var createdUser = await _userRepository.Create(newUser);

        return _mapper.Map<UserDto>(createdUser);
    }

    /// <inheritdoc />
    public async Task<AuthTokenDto> LoginAsync(UserLoginDto user)
    {
        var existingUser = await _userRepository.GetByLoginAsync(user.Login);
        if (existingUser == null || !_passwordHasher.Verify(user.Password, existingUser.PasswordHash))
        {
            throw new ValidationException("Неверный логин или пароль.");
        }

        return await IssueTokensAsync(existingUser);
    }

    /// <inheritdoc />
    public async Task<AuthTokenDto> RefreshAsync(RefreshTokenDto refreshToken)
    {
        var tokenHash = _passwordHasher.Hash(refreshToken.RefreshToken);
        var existingRefreshToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash);

        if (existingRefreshToken == null ||
            existingRefreshToken.RevokedAt != null ||
            existingRefreshToken.ExpiresAt <= DateTime.UtcNow)
        {
            throw new ValidationException("Refresh-токен недействителен.");
        }

        var user = await _userRepository.GetByIdAsync(existingRefreshToken.UserId);
        if (user == null)
        {
            throw new ValidationException("Refresh-токен недействителен.");
        }

        await _refreshTokenRepository.RevokeAsync(existingRefreshToken);

        return await IssueTokensAsync(user);
    }

    /// <inheritdoc />
    public async Task LogoutAsync(RefreshTokenDto refreshToken)
    {
        var tokenHash = _passwordHasher.Hash(refreshToken.RefreshToken);
        var existingRefreshToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash);

        if (existingRefreshToken == null || existingRefreshToken.RevokedAt != null)
        {
            return;
        }

        await _refreshTokenRepository.RevokeAsync(existingRefreshToken);
    }

    private async Task<AuthTokenDto> IssueTokensAsync(UserEntity user)
    {
        var accessToken = _jwtTokenService.Generate(user);
        var refreshToken = GenerateRefreshToken();

        var createdAt = DateTime.UtcNow;

        await _refreshTokenRepository.CreateAsync(new RefreshTokenEntity
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = _passwordHasher.Hash(refreshToken),
            CreatedAt = createdAt,
            ExpiresAt = _jwtTokenService.GetRefreshTokenExpiresAt(createdAt)
        });

        return new AuthTokenDto
        {
            Token = accessToken,
            AccessToken = accessToken,
            RefreshToken = refreshToken
        };
    }

    private static string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}

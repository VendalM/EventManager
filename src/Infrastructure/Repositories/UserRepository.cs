using Application.Interfaces;
using Domain.Models;
using Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// Репозиторий для управления данными пользователей.
/// </summary>
public class UserRepository : IUserRepository
{
    /// <summary>
    /// Контекст базы данных для доступа к данным пользователей.
    /// </summary>
    private readonly AppDbContext _context;

    /// <summary>
    /// Конструктор, который принимает контекст базы данных для взаимодействия с данными пользователей
    /// </summary>
    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<UserEntity?> GetByIdAsync(Guid id)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
    }

    /// <inheritdoc />
    public async Task<UserEntity?> GetByLoginAsync(string login)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Login == login);
    }
    
    /// <inheritdoc />
    public async Task<UserEntity> Create(UserEntity user)
    {
        var entry = await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        return entry.Entity;
    }
}
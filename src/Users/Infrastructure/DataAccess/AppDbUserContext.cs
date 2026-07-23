using Microsoft.EntityFrameworkCore;
using Users.Domain.Models;

namespace Users.Infrastructure.DataAccess;

/// <summary>
/// Контекст базы данных для приложения EventManager
/// </summary>
public class AppDbUserContext : DbContext
{
    /// <summary>
    /// Конструктор, который принимает параметры конфигурации для настройки контекста базы данных
    /// </summary>
    /// <param name="options">Параметры конфигурации для настройки контекста базы данных, передаваемые при регистрации в DI контейнере</param>
    public AppDbUserContext(DbContextOptions<AppDbUserContext> options)
        : base(options)
    {
    }
    
    /// <summary>
    /// Таблица пользователей
    /// </summary>
    public DbSet<UserEntity> Users => Set<UserEntity>();

    /// <summary>
    /// Таблица refresh-токенов.
    /// </summary>
    public DbSet<RefreshTokenEntity> RefreshTokens => Set<RefreshTokenEntity>();
    
    /// <summary>
    /// Объявление правил создания таблиц
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbUserContext).Assembly);
    }
}
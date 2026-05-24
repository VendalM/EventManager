using EventManager.Models;
using Microsoft.EntityFrameworkCore;

namespace EventManager.Infrastructure.DataAccess;

/// <summary>
/// Контекст базы данных для приложения EventManager
/// </summary>
public class AppDbContext : DbContext
{
    /// <summary>
    /// Конструктор, который принимает параметры конфигурации для настройки контекста базы данных
    /// </summary>
    /// <param name="options">Параметры конфигурации для настройки контекста базы данных, передаваемые при регистрации в DI контейнере</param>
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }
    
    /// <summary>
    /// Таблица событий
    /// </summary>
    public DbSet<EventEntity> Events => Set<EventEntity>();
    
    /// <summary>
    /// Таблица бронирований
    /// </summary>
    public DbSet<BookingEntity> Bookings => Set<BookingEntity>();
    
    /// <summary>
    /// Объявление правил создания таблиц
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
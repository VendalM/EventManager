using Events.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Events.Infrastructure.DataAccess;

/// <summary>
/// Контекст базы данных для приложения EventManager
/// </summary>
public class EventsAppDbContext : DbContext
{
    /// <summary>
    /// Конструктор, который принимает параметры конфигурации для настройки контекста базы данных
    /// </summary>
    /// <param name="options">Параметры конфигурации для настройки контекста базы данных, передаваемые при регистрации в DI контейнере</param>
    public EventsAppDbContext(DbContextOptions<EventsAppDbContext> options)
        : base(options)
    {
    }
    
    /// <summary>
    /// Таблица событий
    /// </summary>
    public DbSet<EventEntity> Events => Set<EventEntity>();
    
    /// <summary>
    /// Объявление правил создания таблиц
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EventsAppDbContext).Assembly);
    }
}
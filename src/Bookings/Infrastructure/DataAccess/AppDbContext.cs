using Bookings.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Bookings.Infrastructure.DataAccess;

/// <summary>
/// Контекст базы данных для приложения EventManager
/// </summary>
public class BookingsAppDbContext : DbContext
{
    /// <summary>
    /// Конструктор, который принимает параметры конфигурации для настройки контекста базы данных
    /// </summary>
    /// <param name="options">Параметры конфигурации для настройки контекста базы данных, передаваемые при регистрации в DI контейнере</param>
    public BookingsAppDbContext(DbContextOptions<BookingsAppDbContext> options)
        : base(options)
    {
    }
    
    /// <summary>
    /// Таблица бронирований
    /// </summary>
    public DbSet<BookingEntity> Bookings => Set<BookingEntity>();
    
    /// <summary>
    /// Объявление правил создания таблиц
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BookingsAppDbContext).Assembly);
    }
}
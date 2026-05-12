using EventManager.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventManager.Infrastructure.DataAccess.Configurations;

/// <summary>
/// Конфигурация сущности EventEntity для Entity Framework Core
/// </summary>
public class EventConfiguration : IEntityTypeConfiguration<EventEntity>
{
    /// <summary>
    /// Настройка модели EventEntity для Entity Framework Core
    /// </summary>
    /// <param name="builder">Объект для настройки модели EventEntity</param>
    public void Configure(EntityTypeBuilder<EventEntity> builder)
    {
        builder.ToTable("events", "catalog");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).ValueGeneratedNever();
        builder.Property(b => b.Title).IsRequired().HasMaxLength(200);
        builder.Property(b => b.Description).HasMaxLength(1000);
        builder.Property(b => b.StartDate).IsRequired();
        builder.Property(b => b.EndDate).IsRequired();
        builder.Property(b => b.TotalSeats).IsRequired();
        builder.Property(b => b.AvailableSeats).IsRequired();
        builder.HasMany(e => e.Bookings)
            .WithOne(b => b.Event)
            .HasForeignKey(b => b.EventId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
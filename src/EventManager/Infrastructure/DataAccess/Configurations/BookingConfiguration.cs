using EventManager.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventManager.Infrastructure.DataAccess.Configurations;

/// <summary>
/// Конфигурация сущности BookingEntity для Entity Framework Core
/// </summary>
public class BookingConfiguration: IEntityTypeConfiguration<BookingEntity>
{
    /// <summary>
    /// Настройка модели BookingEntity для Entity Framework Core
    /// </summary>
    /// <param name="builder">Объект для настройки модели BookingEntity</param>
    public void Configure(EntityTypeBuilder<BookingEntity> builder)
    {
        builder.ToTable("bookings", "catalog");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).ValueGeneratedNever();
        builder.Property(b => b.Status).HasConversion<string>();
        builder.HasOne(b => b.Event)
            .WithMany(e => e.Bookings)
            .HasForeignKey(b => b.EventId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
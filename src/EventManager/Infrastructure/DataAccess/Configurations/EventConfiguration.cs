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
        builder.ToTable("events");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(e => e.Title)
            .HasColumnName("title")
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Description)
            .HasColumnName("description")
            .HasMaxLength(2000);

        builder.Property(e => e.StartDate)
            .HasColumnName("start_at")
            .IsRequired();

        builder.Property(e => e.EndDate)
            .HasColumnName("end_at")
            .IsRequired();

        builder.Property(e => e.TotalSeats)
            .HasColumnName("total_seats")
            .IsRequired();

        builder.Property(e => e.AvailableSeats)
            .HasColumnName("available_seats")
            .IsRequired();

        builder.HasMany(e => e.Bookings)
            .WithOne(b => b.Event)
            .HasForeignKey(b => b.EventId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
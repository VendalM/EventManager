using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Users.Domain.Models;

namespace Users.Infrastructure.DataAccess.Configurations;

/// <summary>
/// Конфигурация сущности UserEntity для Entity Framework Core
/// </summary>
public class UserConfiguration: IEntityTypeConfiguration<UserEntity>
{
    /// <summary>
    /// Настройка модели UserEntity для Entity Framework Core
    /// </summary>
    /// <param name="builder">Объект для настройки модели UserEntity</param>
    public void Configure(EntityTypeBuilder<UserEntity> builder)
    {
        builder.ToTable("users");

        builder.HasKey(b => b.Id);

        builder.HasIndex(b => b.Login)
            .IsUnique();

        builder.Property(b => b.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(b => b.Login)
            .HasColumnName("login")
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(b => b.PasswordHash)
            .HasColumnName("password_hash")
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(b => b.Role)
            .HasColumnName("role")
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);
    }
}
using Application.Models;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DataAccess.Configurations;

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

        builder.Property(b => b.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(b => b.Login)
            .HasColumnName("login")
            .IsRequired();
        
        builder.Property(b => b.Password)
            .HasColumnName("password")
            .IsRequired();

        builder.Property(b => b.Role)
            .HasColumnName("status")
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);
    }
}
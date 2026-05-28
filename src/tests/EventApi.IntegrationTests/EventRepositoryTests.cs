using EventManager.Application.Repositories;
using EventManager.Infrastructure.DataAccess;
using EventManager.Models;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace EventApi.IntegrationTests;

/// <summary>
/// Интеграционные тесты для EventRepository с реальной PostgreSQL в контейнере.
/// </summary>
public class EventRepositoryTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    /// <summary>
    /// Запускает контейнер и создаёт схему БД (таблицы events, bookings).
    /// </summary>
    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
    }
    
    /// <summary>
    /// Останавливает и удаляет контейнер после выполнения всех тестов.
    /// </summary>
    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    /// <summary>
    /// Создает соединение с БД.
    /// </summary>
    private AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;
        return new AppDbContext(options);
    }

    /// <summary>
    /// Полностью пересоздаёт тестовую базу данных: сбрасывает пул, завершает соединения,
    /// удаляет и создаёт БД, затем создаёт схему через EnsureCreatedAsync.
    /// </summary>
    private async Task ResetDatabaseAsync()
    {
        await using var context = CreateContext();
        var tableNames = context.Model.GetEntityTypes()
            .Select(t => t.GetTableName())
            .Distinct()
            .ToList();
        if (tableNames.Any())
        {
            var truncateSql = $"TRUNCATE TABLE {string.Join(", ", tableNames)} RESTART IDENTITY CASCADE;";
            await context.Database.ExecuteSqlRawAsync(truncateSql);
        }
    }

    /// <summary>
    /// Приводим время к UTC.
    /// </summary>
    private static DateTime NormalizeToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
    
    /// <summary>
    /// Проверяет, что AddAsync сохраняет событие в БД.
    /// </summary>
    [Fact]
    public async Task AddAsync_ShouldSaveEventToDatabase()
    {
        await ResetDatabaseAsync();
        await using var context = CreateContext();
        var repo = new EventRepository(context);
        var id = Guid.NewGuid();
        var ev = new EventEntity
        {
            Id = id,
            Title = "Конференция",
            Description = "Описание",
            StartDate = NormalizeToUtc(new DateTime(2024, 12, 25, 10, 0, 0)),
            EndDate = NormalizeToUtc(new DateTime(2024, 12, 25, 18, 0, 0)),
            TotalSeats = 100,
            AvailableSeats = 100
        };

        await repo.AddAsync(ev);

        await using var verifyContext = CreateContext();
        var saved = await verifyContext.Events.FirstOrDefaultAsync(e => e.Id == id);
        Assert.NotNull(saved);
        Assert.Equal("Конференция", saved.Title);
    }

    /// <summary>
    /// Проверяет, что GetByIdAsync возвращает событие, если оно существует.
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_WhenEventExists_ShouldReturnEvent()
    {
        await ResetDatabaseAsync();
        await using var context = CreateContext();
        var id = Guid.NewGuid();
        context.Events.Add(new EventEntity
        {
            Id = id,
            Title = "Test",
            StartDate = NormalizeToUtc(DateTime.UtcNow),
            EndDate = NormalizeToUtc(DateTime.UtcNow.AddHours(1)),
            TotalSeats = 10,
            AvailableSeats = 10
        });
        await context.SaveChangesAsync();

        var repo = new EventRepository(CreateContext());
        var result = await repo.GetByIdAsync(id);

        Assert.NotNull(result);
        Assert.Equal("Test", result.Title);
    }

    /// <summary>
    /// Проверяет, что GetByIdAsync возвращает null для несуществующего ID.
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_WhenEventDoesNotExist_ShouldReturnNull()
    {
        await ResetDatabaseAsync();
        var repo = new EventRepository(CreateContext());
        var result = await repo.GetByIdAsync(Guid.NewGuid());
        Assert.Null(result);
    }

    /// <summary>
    /// Проверяет, что UpdateAsync изменяет существующее событие.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_ShouldModifyExistingEvent()
    {
        await ResetDatabaseAsync();
        await using var context = CreateContext();
        var id = Guid.NewGuid();
        context.Events.Add(new EventEntity
        {
            Id = id,
            Title = "Old",
            StartDate = NormalizeToUtc(DateTime.UtcNow),
            EndDate = NormalizeToUtc(DateTime.UtcNow.AddHours(1)),
            TotalSeats = 5,
            AvailableSeats = 5
        });
        await context.SaveChangesAsync();

        var repo = new EventRepository(CreateContext());
        var updated = new EventEntity
        {
            Id = id,
            Title = "New",
            Description = "Updated description",
            StartDate = NormalizeToUtc(DateTime.UtcNow.AddDays(1)),
            EndDate = NormalizeToUtc(DateTime.UtcNow.AddDays(1).AddHours(2)),
            TotalSeats = 20,
            AvailableSeats = 20
        };
        await repo.UpdateAsync(updated);

        await using var verifyContext = CreateContext();
        var changed = await verifyContext.Events.FirstAsync(e => e.Id == id);
        Assert.Equal("New", changed.Title);
        Assert.Equal(20, changed.TotalSeats);
    }

    /// <summary>
    /// Проверяет, что RemoveAsync удаляет существующее событие и возвращает true.
    /// </summary>
    [Fact]
    public async Task RemoveAsync_WhenEventExists_ShouldDeleteAndReturnTrue()
    {
        await ResetDatabaseAsync();
        await using var context = CreateContext();
        var id = Guid.NewGuid();
        context.Events.Add(new EventEntity
        {
            Id = id,
            Title = "ToDelete",
            StartDate = NormalizeToUtc(DateTime.UtcNow),
            EndDate = NormalizeToUtc(DateTime.UtcNow.AddHours(1)),
            TotalSeats = 1,
            AvailableSeats = 1
        });
        await context.SaveChangesAsync();

        var repo = new EventRepository(CreateContext());
        var result = await repo.RemoveAsync(id);

        Assert.True(result);
        await using var verifyContext = CreateContext();
        var exists = await verifyContext.Events.AnyAsync(e => e.Id == id);
        Assert.False(exists);
    }

    /// <summary>
    /// Проверяет, что RemoveAsync возвращает false для несуществующего ID.
    /// </summary>
    [Fact]
    public async Task RemoveAsync_WhenEventDoesNotExist_ShouldReturnFalse()
    {
        await ResetDatabaseAsync();
        var repo = new EventRepository(CreateContext());
        var result = await repo.RemoveAsync(Guid.NewGuid());
        Assert.False(result);
    }

    /// <summary>
    /// Проверяет, что GetAllAsync возвращает все события из БД.
    /// </summary>
    [Fact]
    public async Task GetAllAsync_ShouldReturnAllEvents()
    {
        await ResetDatabaseAsync();
        await using var context = CreateContext();
        context.Events.AddRange(
            new EventEntity { 
                Id = Guid.NewGuid(), 
                Title = "A", 
                StartDate = NormalizeToUtc(DateTime.UtcNow), 
                EndDate = NormalizeToUtc(DateTime.UtcNow.AddHours(1)), 
                TotalSeats = 1, 
                AvailableSeats = 1 
            },
            new EventEntity { 
                Id = Guid.NewGuid(), 
                Title = "B", 
                StartDate = NormalizeToUtc(DateTime.UtcNow), 
                EndDate = NormalizeToUtc(DateTime.UtcNow.AddHours(1)), 
                TotalSeats = 1, 
                AvailableSeats = 1 
            }
        );
        await context.SaveChangesAsync();

        var repo = new EventRepository(CreateContext());
        var result = await repo.GetAllAsync();

        Assert.Equal(2, result.Count);
    }

    /// <summary>
    /// Проверяет, что HasEventAsync возвращает true для существующего события.
    /// </summary>
    [Fact]
    public async Task HasEventAsync_ShouldReturnTrueForExistingEvent()
    {
        await ResetDatabaseAsync();
        await using var context = CreateContext();
        var id = Guid.NewGuid();
        context.Events.Add(new EventEntity
        {
            Id = id,
            Title = "Exists",
            StartDate = NormalizeToUtc(DateTime.UtcNow),
            EndDate = NormalizeToUtc(DateTime.UtcNow.AddHours(1)),
            TotalSeats = 1,
            AvailableSeats = 1
        });
        await context.SaveChangesAsync();

        var repo = new EventRepository(CreateContext());
        var result = await repo.HasEventAsync(id);
        Assert.True(result);
    }

    /// <summary>
    /// Проверяет, что HasEventAsync возвращает false для несуществующего события.
    /// </summary>
    [Fact]
    public async Task HasEventAsync_ShouldReturnFalseForMissingEvent()
    {
        await ResetDatabaseAsync();
        var repo = new EventRepository(CreateContext());
        var result = await repo.HasEventAsync(Guid.NewGuid());
        Assert.False(result);
    }
    
    /// <summary>
    /// Проверяет, что база данных отклоняет вставку события с пустым Title (NOT NULL).
    /// </summary>
    [Fact]
    public async Task AddAsync_WhenTitleIsNull_ShouldThrowDbUpdateException()
    {
        await ResetDatabaseAsync();
        var invalidEvent = new EventEntity
        {
            Id = Guid.NewGuid(),
            Title = null!,
            StartDate = NormalizeToUtc(DateTime.UtcNow),
            EndDate = NormalizeToUtc(DateTime.UtcNow.AddHours(1)),
            TotalSeats = 10,
            AvailableSeats = 10
        };
        var repo = new EventRepository(CreateContext());
        await Assert.ThrowsAsync<DbUpdateException>(() => repo.AddAsync(invalidEvent));
    }
}
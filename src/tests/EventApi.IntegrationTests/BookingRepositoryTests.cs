using EventManager.Application.Repositories;
using EventManager.Enums;
using EventManager.Infrastructure.DataAccess;
using EventManager.Models;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace EventApi.IntegrationTests;

/// <summary>
/// Интеграционные тесты для BookingRepository с реальной PostgreSQL в контейнере.
/// </summary>
public class BookingRepositoryTests : IAsyncLifetime
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
    /// Очищает все данные из таблиц (events, bookings), не удаляя схему.
    /// Используется перед каждым тестом для изоляции.
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
    /// Создаем тестовое событие для привязывания брони.
    /// </summary>
    private async Task<Guid> CreateTestEventAsync()
    {
        await using var context = CreateContext();
        var eventId = Guid.NewGuid();
        context.Events.Add(new EventEntity
        {
            Id = eventId,
            Title = "Test Event",
            StartDate = NormalizeToUtc(DateTime.UtcNow.AddDays(1)),
            EndDate = NormalizeToUtc(DateTime.UtcNow.AddDays(1).AddHours(2)),
            TotalSeats = 10,
            AvailableSeats = 10
        });
        await context.SaveChangesAsync();
        return eventId;
    }
    
    /// <summary>
    /// Проверяет, что AddAsync сохраняет бронирование в БД.
    /// </summary>
    [Fact]
    public async Task AddAsync_ShouldSaveBookingToDatabase()
    {
        // Arrange
        await ResetDatabaseAsync();
        var eventId = await CreateTestEventAsync();
        var bookingId = Guid.NewGuid();
        var booking = new BookingEntity
        {
            Id = bookingId,
            EventId = eventId,
            Status = BookingStatus.Pending,
            CreatedAt = NormalizeToUtc(DateTime.UtcNow)
        };

        // Act
        await using var context = CreateContext();
        var repo = new BookingRepository(context);
        await repo.AddAsync(booking);

        // Assert
        await using var verifyContext = CreateContext();
        var saved = await verifyContext.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId);
        Assert.NotNull(saved);
        Assert.Equal(BookingStatus.Pending, saved.Status);
        Assert.Equal(eventId, saved.EventId);
    }

    /// <summary>
    /// Проверяет, что GetByIdAsync возвращает бронирование, если оно существует.
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_WhenBookingExists_ShouldReturnBooking()
    {
        // Arrange
        await ResetDatabaseAsync();
        var eventId = await CreateTestEventAsync();
        var bookingId = Guid.NewGuid();
        await using var arrangeContext = CreateContext();
        arrangeContext.Bookings.Add(new BookingEntity
        {
            Id = bookingId,
            EventId = eventId,
            Status = BookingStatus.Confirmed,
            CreatedAt = NormalizeToUtc(DateTime.UtcNow)
        });
        await arrangeContext.SaveChangesAsync();

        // Act
        var repo = new BookingRepository(CreateContext());
        var result = await repo.GetByIdAsync(bookingId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(bookingId, result.Id);
        Assert.Equal(BookingStatus.Confirmed, result.Status);
    }

    /// <summary>
    /// Проверяет, что GetByIdAsync возвращает null для несуществующего ID.
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_WhenBookingDoesNotExist_ShouldReturnNull()
    {
        await ResetDatabaseAsync();
        var repo = new BookingRepository(CreateContext());
        var result = await repo.GetByIdAsync(Guid.NewGuid());
        Assert.Null(result);
    }

    /// <summary>
    /// Проверяет, что UpdateAsync изменяет существующее бронирование.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_ShouldModifyExistingBooking()
    {
        // Arrange
        await ResetDatabaseAsync();
        var eventId = await CreateTestEventAsync();
        var bookingId = Guid.NewGuid();
        await using var arrangeContext = CreateContext();
        arrangeContext.Bookings.Add(new BookingEntity
        {
            Id = bookingId,
            EventId = eventId,
            Status = BookingStatus.Pending,
            CreatedAt = NormalizeToUtc(DateTime.UtcNow),
            ProcessedAt = null
        });
        await arrangeContext.SaveChangesAsync();

        // Act
        var repo = new BookingRepository(CreateContext());
        var updatedBooking = new BookingEntity
        {
            Id = bookingId,
            EventId = eventId,
            Status = BookingStatus.Rejected,
            CreatedAt = NormalizeToUtc(DateTime.UtcNow.AddMinutes(-10)),
            ProcessedAt = NormalizeToUtc(DateTime.UtcNow)
        };
        await repo.UpdateAsync(updatedBooking);

        // Assert
        await using var verifyContext = CreateContext();
        var changed = await verifyContext.Bookings.FirstAsync(b => b.Id == bookingId);
        Assert.Equal(BookingStatus.Rejected, changed.Status);
        Assert.NotNull(changed.ProcessedAt);
    }

    /// <summary>
    /// Проверяет, что UpdateAsync не падает и не меняет ничего при несуществующем бронировании.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_WhenBookingDoesNotExist_ShouldNotThrowAndNotChangeDatabase()
    {
        // Arrange
        await ResetDatabaseAsync();
        var nonExistentId = Guid.NewGuid();
        var fakeBooking = new BookingEntity
        {
            Id = nonExistentId,
            EventId = Guid.NewGuid(),
            Status = BookingStatus.Pending,
            CreatedAt = NormalizeToUtc(DateTime.UtcNow)
        };

        // Act & Assert
        var repo = new BookingRepository(CreateContext());
        await repo.UpdateAsync(fakeBooking);
        await using var verifyContext = CreateContext();
        var exists = await verifyContext.Bookings.AnyAsync(b => b.Id == nonExistentId);
        Assert.False(exists);
    }

    /// <summary>
    /// Проверяет, что GetAllAsync возвращает все бронирования из БД.
    /// </summary>
    [Fact]
    public async Task GetAllAsync_ShouldReturnAllBookings()
    {
        // Arrange
        await ResetDatabaseAsync();
        var eventId = await CreateTestEventAsync();
        await using var arrangeContext = CreateContext();
        arrangeContext.Bookings.AddRange(
            new BookingEntity { Id = Guid.NewGuid(), EventId = eventId, Status = BookingStatus.Pending, CreatedAt = NormalizeToUtc(DateTime.UtcNow) },
            new BookingEntity { Id = Guid.NewGuid(), EventId = eventId, Status = BookingStatus.Confirmed, CreatedAt = NormalizeToUtc(DateTime.UtcNow) }
        );
        await arrangeContext.SaveChangesAsync();

        // Act
        var repo = new BookingRepository(CreateContext());
        var result = await repo.GetAllAsync();

        // Assert
        Assert.Equal(2, result.Count);
    }
    
    /// <summary>
    /// Проверяет, что база данных отклоняет вставку бронирования с несуществующим EventId (нарушение внешнего ключа).
    /// </summary>
    [Fact]
    public async Task AddAsync_WhenEventIdDoesNotExist_ShouldThrowDbUpdateException()
    {
        // Arrange
        await ResetDatabaseAsync();
        var invalidBooking = new BookingEntity
        {
            Id = Guid.NewGuid(),
            EventId = Guid.NewGuid(), // несуществующий EventId
            Status = BookingStatus.Pending,
            CreatedAt = NormalizeToUtc(DateTime.UtcNow)
        };

        // Act & Assert
        var repo = new BookingRepository(CreateContext());
        await Assert.ThrowsAsync<DbUpdateException>(() => repo.AddAsync(invalidBooking));
    }

    /// <summary>
    /// Проверяет, что Status преобразуется в строку через Enum-конвертер (не вызовет исключения).
    /// </summary>
    [Fact]
    public async Task AddAsync_ShouldStoreAndRetrieveStatusAsEnum()
    {
        await ResetDatabaseAsync();
        var eventId = await CreateTestEventAsync();
        var bookingId = Guid.NewGuid();
        var booking = new BookingEntity
        {
            Id = bookingId,
            EventId = eventId,
            Status = BookingStatus.Confirmed,
            CreatedAt = NormalizeToUtc(DateTime.UtcNow)
        };

        await using var context = CreateContext();
        var repo = new BookingRepository(context);
        await repo.AddAsync(booking);

        await using var verifyContext = CreateContext();
        var retrieved = await verifyContext.Bookings.FirstAsync(b => b.Id == bookingId);
        Assert.Equal(BookingStatus.Confirmed, retrieved.Status);
    }
}
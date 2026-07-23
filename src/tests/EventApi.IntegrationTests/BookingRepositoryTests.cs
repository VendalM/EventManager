using Bookings.Domain.Models;
using Bookings.Infrastructure.DataAccess;
using Bookings.Infrastructure.Repositories;
using Contracts.Bookings;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace EventApi.IntegrationTests;

/// <summary>
/// Интеграционные тесты репозитория Bookings на реальном PostgreSQL-контейнере.
/// </summary>
public class BookingRepositoryTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }

    /// <summary>
    /// Проверяет, что бронь сохраняется в собственной БД Bookings без таблиц Users и Events.
    /// </summary>
    [Fact]
    public async Task AddAsync_SavesBookingWithoutUsersOrEventsTables()
    {
        await ResetDatabaseAsync();
        await using var context = CreateContext();
        var repository = new BookingRepository(context);
        var booking = CreateBooking(BookingStatus.Pending);

        await repository.AddAsync(booking);

        await using var verifyContext = CreateContext();
        var saved = await verifyContext.Bookings.FirstOrDefaultAsync(x => x.Id == booking.Id);

        Assert.NotNull(saved);
        Assert.Equal(booking.EventId, saved!.EventId);
        Assert.Equal(booking.UserId, saved.UserId);
        Assert.Equal(BookingStatus.Pending, saved.Status);
    }

    /// <summary>
    /// Проверяет сохранение финального статуса и времени обработки брони.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_ChangesStatusAndProcessedAt()
    {
        await ResetDatabaseAsync();
        var booking = CreateBooking(BookingStatus.Pending);
        await SeedBookingAsync(booking);
        var processedAt = DateTime.UtcNow;
        var repository = new BookingRepository(CreateContext());

        booking.Status = BookingStatus.Confirmed;
        booking.ProcessedAt = processedAt;

        await repository.UpdateAsync(booking);

        await using var verifyContext = CreateContext();
        var saved = await verifyContext.Bookings.FirstAsync(x => x.Id == booking.Id);

        Assert.Equal(BookingStatus.Confirmed, saved.Status);
        Assert.NotNull(saved.ProcessedAt);
        Assert.True(
            (saved.ProcessedAt.Value - processedAt).Duration() < TimeSpan.FromMilliseconds(1),
            "PostgreSQL stores timestamp values with microsecond precision.");
    }

    /// <summary>
    /// Проверяет, что лимит активных броней учитывает только Pending и Confirmed.
    /// </summary>
    [Fact]
    public async Task HasReachedActiveBookingsLimitAsync_CountsOnlyPendingAndConfirmed()
    {
        await ResetDatabaseAsync();
        var userId = Guid.NewGuid();
        var repository = new BookingRepository(CreateContext());

        await SeedBookingAsync(CreateBooking(BookingStatus.Pending, userId));
        await SeedBookingAsync(CreateBooking(BookingStatus.Confirmed, userId));
        await SeedBookingAsync(CreateBooking(BookingStatus.Rejected, userId));
        await SeedBookingAsync(CreateBooking(BookingStatus.Cancelled, userId));

        var limitReached = await repository.HasReachedActiveBookingsLimitAsync(userId);

        Assert.False(limitReached);
    }

    /// <summary>
    /// Проверяет, что репозиторий считает лимит достигнутым при максимальном количестве активных броней.
    /// </summary>
    [Fact]
    public async Task HasReachedActiveBookingsLimitAsync_ReturnsTrueAtLimit()
    {
        await ResetDatabaseAsync();
        var userId = Guid.NewGuid();
        var repository = new BookingRepository(CreateContext());

        for (var i = 0; i < repository.GetActiveBookingsLimit(); i++)
        {
            await SeedBookingAsync(CreateBooking(BookingStatus.Pending, userId));
        }

        var limitReached = await repository.HasReachedActiveBookingsLimitAsync(userId);

        Assert.True(limitReached);
    }

    /// <summary>
    /// Проверяет чтение всех броней из отдельной БД Bookings.
    /// </summary>
    [Fact]
    public async Task GetAllAsync_ReturnsAllBookingsFromBookingsDatabase()
    {
        await ResetDatabaseAsync();
        await SeedBookingAsync(CreateBooking(BookingStatus.Pending));
        await SeedBookingAsync(CreateBooking(BookingStatus.Confirmed));
        var repository = new BookingRepository(CreateContext());

        var result = await repository.GetAllAsync();

        Assert.Equal(2, result.Count);
    }

    private BookingsAppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<BookingsAppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        return new BookingsAppDbContext(options);
    }

    private async Task ResetDatabaseAsync()
    {
        await using var context = CreateContext();
        context.Bookings.RemoveRange(context.Bookings);
        await context.SaveChangesAsync();
    }

    private async Task SeedBookingAsync(BookingEntity booking)
    {
        await using var context = CreateContext();
        context.Bookings.Add(booking);
        await context.SaveChangesAsync();
    }

    private static BookingEntity CreateBooking(BookingStatus status, Guid? userId = null)
    {
        return new BookingEntity
        {
            Id = Guid.NewGuid(),
            EventId = Guid.NewGuid(),
            UserId = userId ?? Guid.NewGuid(),
            Status = status,
            CreatedAt = DateTime.UtcNow.AddMinutes(-10),
            ProcessedAt = status == BookingStatus.Pending ? null : DateTime.UtcNow.AddMinutes(-5)
        };
    }
}

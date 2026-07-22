using Events.Domain.Models;
using Events.Infrastructure.DataAccess;
using Events.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace EventApi.IntegrationTests;

/// <summary>
/// Интеграционные тесты репозитория Events на реальном PostgreSQL-контейнере.
/// </summary>
public class EventRepositoryTests : IAsyncLifetime
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
    /// Проверяет сохранение события в собственной БД Events.
    /// </summary>
    [Fact]
    public async Task AddAsync_SavesEventToEventsDatabase()
    {
        await ResetDatabaseAsync();
        await using var context = CreateContext();
        var repository = new EventRepository(context);
        var eventEntity = CreateEvent();

        await repository.AddAsync(eventEntity);

        await using var verifyContext = CreateContext();
        var saved = await verifyContext.Events.FirstOrDefaultAsync(x => x.Id == eventEntity.Id);

        Assert.NotNull(saved);
        Assert.Equal(eventEntity.Title, saved!.Title);
        Assert.Equal(eventEntity.TotalSeats, saved.TotalSeats);
        Assert.Equal(eventEntity.AvailableSeats, saved.AvailableSeats);
    }

    /// <summary>
    /// Проверяет обновление основных полей события и количества доступных мест.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_UpdatesSeatsAndMainFields()
    {
        await ResetDatabaseAsync();
        var eventEntity = CreateEvent(totalSeats: 10, availableSeats: 10);
        await SeedEventAsync(eventEntity);
        var repository = new EventRepository(CreateContext());

        eventEntity.Title = "Updated event";
        eventEntity.TotalSeats = 10;
        eventEntity.AvailableSeats = 9;

        await repository.UpdateAsync(eventEntity);

        await using var verifyContext = CreateContext();
        var saved = await verifyContext.Events.FirstAsync(x => x.Id == eventEntity.Id);

        Assert.Equal("Updated event", saved.Title);
        Assert.Equal(9, saved.AvailableSeats);
    }

    /// <summary>
    /// Проверяет быстрый поиск существования события по идентификатору.
    /// </summary>
    [Fact]
    public async Task HasEventAsync_ReturnsTrueOnlyForExistingEvent()
    {
        await ResetDatabaseAsync();
        var eventEntity = CreateEvent();
        await SeedEventAsync(eventEntity);
        var repository = new EventRepository(CreateContext());

        var existing = await repository.HasEventAsync(eventEntity.Id);
        var missing = await repository.HasEventAsync(Guid.NewGuid());

        Assert.True(existing);
        Assert.False(missing);
    }

    /// <summary>
    /// Проверяет удаление события из БД Events.
    /// </summary>
    [Fact]
    public async Task RemoveAsync_DeletesExistingEvent()
    {
        await ResetDatabaseAsync();
        var eventEntity = CreateEvent();
        await SeedEventAsync(eventEntity);
        var repository = new EventRepository(CreateContext());

        var removed = await repository.RemoveAsync(eventEntity.Id);

        Assert.True(removed);
        await using var verifyContext = CreateContext();
        Assert.False(await verifyContext.Events.AnyAsync(x => x.Id == eventEntity.Id));
    }

    private EventsAppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<EventsAppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        return new EventsAppDbContext(options);
    }

    private async Task ResetDatabaseAsync()
    {
        await using var context = CreateContext();
        context.Events.RemoveRange(context.Events);
        await context.SaveChangesAsync();
    }

    private async Task SeedEventAsync(EventEntity eventEntity)
    {
        await using var context = CreateContext();
        context.Events.Add(eventEntity);
        await context.SaveChangesAsync();
    }

    private static EventEntity CreateEvent(int totalSeats = 20, int availableSeats = 20)
    {
        return new EventEntity
        {
            Id = Guid.NewGuid(),
            Title = "Test event",
            Description = "Repository integration test",
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(1).AddHours(2),
            TotalSeats = totalSeats,
            AvailableSeats = availableSeats
        };
    }
}

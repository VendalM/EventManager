using AutoMapper;
using Contracts.Enums;
using Contracts.Messages;
using Events.Application.Interfaces;
using Events.Application.Models;
using Events.Application.Services;
using Events.Domain.Models;
using Events.Infrastructure.Mappers;
using Moq;

namespace EventManager.Tests;

/// <summary>
/// Unit-тесты прикладного сервиса Events: проверяют управление событиями и реакцию на сообщения от Bookings.
/// </summary>
public class EventServiceTests
{
    private readonly IMapper _mapper;

    public EventServiceTests()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<EventMappingProfile>());
        _mapper = config.CreateMapper();
    }

    /// <summary>
    /// Проверяет, что при создании события количество доступных мест равно общей вместимости.
    /// </summary>
    [Fact]
    public async Task Create_SetsAvailableSeatsEqualToTotalSeats()
    {
        EventEntity? savedEvent = null;
        var (service, repository, _) = CreateService();
        repository.Setup(x => x.AddAsync(It.IsAny<EventEntity>()))
            .Callback<EventEntity>(entity => savedEvent = entity)
            .Returns(Task.CompletedTask);

        var result = await service.Create(new EventSaveDto
        {
            Title = "Conference",
            Description = "Tech event",
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(1).AddHours(2),
            TotalSeats = 25
        });

        Assert.Equal(25, result.TotalSeats);
        Assert.Equal(25, result.AvailableSeats);
        Assert.NotNull(savedEvent);
        Assert.Equal(savedEvent!.TotalSeats, savedEvent.AvailableSeats);
    }

    /// <summary>
    /// Проверяет успешную обработку BookingRequested: Events резервирует место и публикует BookingConfirmed.
    /// </summary>
    [Fact]
    public async Task BookingRequested_WhenSeatsAvailable_ReservesSeatAndPublishesConfirmed()
    {
        var eventEntity = CreateFutureEvent(availableSeats: 2);
        var request = CreateBookingRequested(eventEntity.Id);
        EventEntity? updatedEvent = null;
        BookingConfirmed? confirmedMessage = null;
        var (service, repository, publisher) = CreateService();

        repository.Setup(x => x.GetByIdAsync(eventEntity.Id)).ReturnsAsync(eventEntity);
        repository.Setup(x => x.UpdateAsync(It.IsAny<EventEntity>()))
            .Callback<EventEntity>(entity => updatedEvent = entity)
            .Returns(Task.CompletedTask);
        publisher.Setup(x => x.PublishBookingConfirmedAsync(It.IsAny<BookingConfirmed>(), It.IsAny<CancellationToken>()))
            .Callback<BookingConfirmed, CancellationToken>((message, _) => confirmedMessage = message)
            .Returns(Task.CompletedTask);

        await service.BookingRequested(request);

        Assert.NotNull(updatedEvent);
        Assert.Equal(1, updatedEvent!.AvailableSeats);

        Assert.NotNull(confirmedMessage);
        Assert.Equal(request.BookingId, confirmedMessage!.BookingId);
        Assert.Equal(request.EventId, confirmedMessage.EventId);
        Assert.Equal(request.UserId, confirmedMessage.UserId);
        Assert.Equal(request.SeatsCount, confirmedMessage.SeatsCount);
        publisher.Verify(x => x.PublishBookingRejectedAsync(It.IsAny<BookingRejected>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Проверяет, что запрос брони для несуществующего события отклоняется сообщением BookingRejected.
    /// </summary>
    [Fact]
    public async Task BookingRequested_WhenEventDoesNotExist_PublishesRejected()
    {
        var request = CreateBookingRequested(Guid.NewGuid());
        BookingRejected? rejectedMessage = null;
        var (service, repository, publisher) = CreateService();

        repository.Setup(x => x.GetByIdAsync(request.EventId)).ReturnsAsync((EventEntity?)null);
        publisher.Setup(x => x.PublishBookingRejectedAsync(It.IsAny<BookingRejected>(), It.IsAny<CancellationToken>()))
            .Callback<BookingRejected, CancellationToken>((message, _) => rejectedMessage = message)
            .Returns(Task.CompletedTask);

        await service.BookingRequested(request);

        Assert.NotNull(rejectedMessage);
        Assert.Equal(BookingRejectionReason.EventNotFound, rejectedMessage!.Reason);
        repository.Verify(x => x.UpdateAsync(It.IsAny<EventEntity>()), Times.Never);
        publisher.Verify(x => x.PublishBookingConfirmedAsync(It.IsAny<BookingConfirmed>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Проверяет, что нельзя подтвердить бронь на событие, которое уже началось.
    /// </summary>
    [Fact]
    public async Task BookingRequested_WhenEventAlreadyStarted_PublishesRejected()
    {
        var eventEntity = new EventEntity
        {
            Id = Guid.NewGuid(),
            Title = "Started event",
            StartDate = DateTime.UtcNow.AddMinutes(-1),
            EndDate = DateTime.UtcNow.AddHours(1),
            TotalSeats = 10,
            AvailableSeats = 10
        };
        var request = CreateBookingRequested(eventEntity.Id);
        BookingRejected? rejectedMessage = null;
        var (service, repository, publisher) = CreateService();

        repository.Setup(x => x.GetByIdAsync(eventEntity.Id)).ReturnsAsync(eventEntity);
        publisher.Setup(x => x.PublishBookingRejectedAsync(It.IsAny<BookingRejected>(), It.IsAny<CancellationToken>()))
            .Callback<BookingRejected, CancellationToken>((message, _) => rejectedMessage = message)
            .Returns(Task.CompletedTask);

        await service.BookingRequested(request);

        Assert.NotNull(rejectedMessage);
        Assert.Equal(BookingRejectionReason.EventAlreadyStarted, rejectedMessage!.Reason);
        repository.Verify(x => x.UpdateAsync(It.IsAny<EventEntity>()), Times.Never);
        publisher.Verify(x => x.PublishBookingConfirmedAsync(It.IsAny<BookingConfirmed>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Проверяет, что при отсутствии мест Events не меняет событие и публикует отказ.
    /// </summary>
    [Fact]
    public async Task BookingRequested_WhenNoSeats_PublishesRejected()
    {
        var eventEntity = CreateFutureEvent(availableSeats: 0);
        var request = CreateBookingRequested(eventEntity.Id);
        BookingRejected? rejectedMessage = null;
        var (service, repository, publisher) = CreateService();

        repository.Setup(x => x.GetByIdAsync(eventEntity.Id)).ReturnsAsync(eventEntity);
        publisher.Setup(x => x.PublishBookingRejectedAsync(It.IsAny<BookingRejected>(), It.IsAny<CancellationToken>()))
            .Callback<BookingRejected, CancellationToken>((message, _) => rejectedMessage = message)
            .Returns(Task.CompletedTask);

        await service.BookingRequested(request);

        Assert.NotNull(rejectedMessage);
        Assert.Equal(BookingRejectionReason.NoAvailableSeats, rejectedMessage!.Reason);
        Assert.Equal(0, eventEntity.AvailableSeats);
        repository.Verify(x => x.UpdateAsync(It.IsAny<EventEntity>()), Times.Never);
        publisher.Verify(x => x.PublishBookingConfirmedAsync(It.IsAny<BookingConfirmed>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Проверяет обработку BookingCancelled: Events возвращает место и сохраняет новое количество доступных мест.
    /// </summary>
    [Fact]
    public async Task BookingCancelled_WhenEventExists_ReleasesSeatAndPersists()
    {
        var eventEntity = CreateFutureEvent(totalSeats: 3, availableSeats: 1);
        var message = new BookingCancelled(Guid.NewGuid(), eventEntity.Id, Guid.NewGuid(), 1, DateTime.UtcNow);
        EventEntity? updatedEvent = null;
        var (service, repository, _) = CreateService();

        repository.Setup(x => x.GetByIdAsync(eventEntity.Id)).ReturnsAsync(eventEntity);
        repository.Setup(x => x.UpdateAsync(It.IsAny<EventEntity>()))
            .Callback<EventEntity>(entity => updatedEvent = entity)
            .Returns(Task.CompletedTask);

        await service.BookingCancelled(message);

        Assert.NotNull(updatedEvent);
        Assert.Equal(2, updatedEvent!.AvailableSeats);
    }

    /// <summary>
    /// Проверяет защиту от превышения TotalSeats при повторной или лишней отмене брони.
    /// </summary>
    [Fact]
    public async Task BookingCancelled_WhenReleaseWouldExceedTotalSeats_ClampsAvailableSeats()
    {
        var eventEntity = CreateFutureEvent(totalSeats: 3, availableSeats: 3);
        var message = new BookingCancelled(Guid.NewGuid(), eventEntity.Id, Guid.NewGuid(), 1, DateTime.UtcNow);
        EventEntity? updatedEvent = null;
        var (service, repository, _) = CreateService();

        repository.Setup(x => x.GetByIdAsync(eventEntity.Id)).ReturnsAsync(eventEntity);
        repository.Setup(x => x.UpdateAsync(It.IsAny<EventEntity>()))
            .Callback<EventEntity>(entity => updatedEvent = entity)
            .Returns(Task.CompletedTask);

        await service.BookingCancelled(message);

        Assert.NotNull(updatedEvent);
        Assert.Equal(3, updatedEvent!.AvailableSeats);
    }

    private (EventService service, Mock<IEventRepository> repository, Mock<IEventsEventPublisher> publisher) CreateService()
    {
        var repository = new Mock<IEventRepository>();
        var publisher = new Mock<IEventsEventPublisher>();

        repository.Setup(x => x.AddAsync(It.IsAny<EventEntity>())).Returns(Task.CompletedTask);
        repository.Setup(x => x.UpdateAsync(It.IsAny<EventEntity>())).Returns(Task.CompletedTask);
        publisher.Setup(x => x.PublishBookingConfirmedAsync(It.IsAny<BookingConfirmed>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        publisher.Setup(x => x.PublishBookingRejectedAsync(It.IsAny<BookingRejected>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return (new EventService(_mapper, repository.Object, publisher.Object), repository, publisher);
    }

    private static EventEntity CreateFutureEvent(int totalSeats = 2, int availableSeats = 2)
    {
        return new EventEntity
        {
            Id = Guid.NewGuid(),
            Title = "Future event",
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(1).AddHours(2),
            TotalSeats = totalSeats,
            AvailableSeats = availableSeats
        };
    }

    private static BookingRequested CreateBookingRequested(Guid eventId)
    {
        return new BookingRequested(
            Guid.NewGuid(),
            eventId,
            Guid.NewGuid(),
            1,
            DateTime.UtcNow);
    }
}

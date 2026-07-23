using AutoMapper;
using Bookings.Application.Interfaces;
using Bookings.Application.Services;
using Bookings.Domain.Exceptions;
using Bookings.Domain.Models;
using Bookings.Infrastructure.Mappers;
using Contracts.Bookings;
using Contracts.Enums;
using Contracts.Messages;
using Moq;
using BookingValidationException = Bookings.Application.Exceptions.ValidationException;

namespace EventManager.Tests;

/// <summary>
/// Unit-тесты прикладного сервиса Bookings: проверяют только логику броней и публикацию сообщений.
/// </summary>
public class BookingServiceTests
{
    private readonly IMapper _mapper;

    public BookingServiceTests()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<BookingsMappingProfile>());
        _mapper = config.CreateMapper();
    }

    /// <summary>
    /// Проверяет, что Bookings сохраняет новую бронь как Pending и только после сохранения публикует BookingRequested.
    /// </summary>
    [Fact]
    public async Task CreateBookingAsync_WhenLimitNotReached_SavesPendingBookingAndPublishesRequest()
    {
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var operations = new List<string>();
        BookingEntity? savedBooking = null;
        BookingRequested? publishedMessage = null;
        var (service, repository, publisher) = CreateService();

        repository.Setup(x => x.AddAsync(It.IsAny<BookingEntity>()))
            .Callback<BookingEntity>(booking =>
            {
                savedBooking = booking;
                operations.Add("saved");
            })
            .Returns(Task.CompletedTask);

        publisher.Setup(x => x.PublishBookingRequestedAsync(It.IsAny<BookingRequested>(), It.IsAny<CancellationToken>()))
            .Callback<BookingRequested, CancellationToken>((message, _) =>
            {
                publishedMessage = message;
                operations.Add("published");
            })
            .Returns(Task.CompletedTask);

        var result = await service.CreateBookingAsync(eventId, userId);

        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result!.Id);
        Assert.Equal(eventId, result.EventId);
        Assert.Equal(userId, result.UserId);
        Assert.Equal(BookingStatus.Pending, result.Status);
        Assert.Null(result.ProcessedAt);

        Assert.NotNull(savedBooking);
        Assert.Equal(BookingStatus.Pending, savedBooking!.Status);

        Assert.NotNull(publishedMessage);
        Assert.Equal(result.Id, publishedMessage!.BookingId);
        Assert.Equal(eventId, publishedMessage.EventId);
        Assert.Equal(userId, publishedMessage.UserId);
        Assert.Equal(1, publishedMessage.SeatsCount);
        Assert.Equal(new[] { "saved", "published" }, operations);
    }

    /// <summary>
    /// Проверяет, что при превышении лимита активных броней новая бронь не сохраняется и сообщение не публикуется.
    /// </summary>
    [Fact]
    public async Task CreateBookingAsync_WhenActiveLimitReached_ThrowsAndDoesNotPublish()
    {
        var (service, repository, publisher) = CreateService();
        var userId = Guid.NewGuid();

        repository.Setup(x => x.HasReachedActiveBookingsLimitAsync(userId)).ReturnsAsync(true);
        repository.Setup(x => x.GetActiveBookingsLimit()).Returns(10);

        await Assert.ThrowsAsync<ActiveBookingsLimitExceededException>(
            () => service.CreateBookingAsync(Guid.NewGuid(), userId));

        repository.Verify(x => x.AddAsync(It.IsAny<BookingEntity>()), Times.Never);
        publisher.Verify(x => x.PublishBookingRequestedAsync(It.IsAny<BookingRequested>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Проверяет обработку BookingConfirmed: Pending-бронь переводится в Confirmed.
    /// </summary>
    [Fact]
    public async Task BookingConfirmed_WhenBookingIsPending_UpdatesStatusToConfirmed()
    {
        var booking = CreateBooking(BookingStatus.Pending);
        var confirmedAt = DateTime.UtcNow;
        BookingEntity? updatedBooking = null;
        var (service, repository, _) = CreateService();

        repository.Setup(x => x.GetByIdAsync(booking.Id)).ReturnsAsync(booking);
        repository.Setup(x => x.UpdateAsync(It.IsAny<BookingEntity>()))
            .Callback<BookingEntity>(entity => updatedBooking = entity)
            .Returns(Task.CompletedTask);

        await service.BookingConfirmed(new BookingConfirmed(
            booking.Id,
            booking.EventId,
            booking.UserId,
            1,
            confirmedAt));

        Assert.NotNull(updatedBooking);
        Assert.Equal(BookingStatus.Confirmed, updatedBooking!.Status);
        Assert.Equal(confirmedAt, updatedBooking.ProcessedAt);
    }

    /// <summary>
    /// Проверяет обработку BookingRejected: Pending-бронь переводится в Rejected с временем обработки.
    /// </summary>
    [Fact]
    public async Task BookingRejected_WhenBookingIsPending_UpdatesStatusToRejected()
    {
        var booking = CreateBooking(BookingStatus.Pending);
        var rejectedAt = DateTime.UtcNow;
        BookingEntity? updatedBooking = null;
        var (service, repository, _) = CreateService();

        repository.Setup(x => x.GetByIdAsync(booking.Id)).ReturnsAsync(booking);
        repository.Setup(x => x.UpdateAsync(It.IsAny<BookingEntity>()))
            .Callback<BookingEntity>(entity => updatedBooking = entity)
            .Returns(Task.CompletedTask);

        await service.BookingRejected(new BookingRejected(
            booking.Id,
            booking.EventId,
            booking.UserId,
            1,
            BookingRejectionReason.NoAvailableSeats,
            rejectedAt));

        Assert.NotNull(updatedBooking);
        Assert.Equal(BookingStatus.Rejected, updatedBooking!.Status);
        Assert.Equal(rejectedAt, updatedBooking.ProcessedAt);
    }

    /// <summary>
    /// Проверяет идемпотентность обработки подтверждения: уже обработанная бронь повторно не меняется.
    /// </summary>
    [Fact]
    public async Task BookingConfirmed_WhenBookingIsNotPending_DoesNotUpdate()
    {
        var booking = CreateBooking(BookingStatus.Cancelled);
        var (service, repository, _) = CreateService();

        repository.Setup(x => x.GetByIdAsync(booking.Id)).ReturnsAsync(booking);

        await service.BookingConfirmed(new BookingConfirmed(
            booking.Id,
            booking.EventId,
            booking.UserId,
            1,
            DateTime.UtcNow));

        repository.Verify(x => x.UpdateAsync(It.IsAny<BookingEntity>()), Times.Never);
    }

    /// <summary>
    /// Проверяет отмену подтвержденной брони: статус меняется на Cancelled и публикуется BookingCancelled.
    /// </summary>
    [Fact]
    public async Task CancelBookingAsync_WhenBookingIsConfirmed_CancelsAndPublishesMessage()
    {
        var booking = CreateBooking(BookingStatus.Confirmed);
        BookingEntity? updatedBooking = null;
        BookingCancelled? publishedMessage = null;
        var (service, repository, publisher) = CreateService();

        repository.Setup(x => x.GetByIdAsync(booking.Id)).ReturnsAsync(booking);
        repository.Setup(x => x.UpdateAsync(It.IsAny<BookingEntity>()))
            .Callback<BookingEntity>(entity => updatedBooking = entity)
            .Returns(Task.CompletedTask);
        publisher.Setup(x => x.PublishBookingCancelledAsync(It.IsAny<BookingCancelled>(), It.IsAny<CancellationToken>()))
            .Callback<BookingCancelled, CancellationToken>((message, _) => publishedMessage = message)
            .Returns(Task.CompletedTask);

        var result = await service.CancelBookingAsync(booking.Id, booking.UserId, isAdmin: false);

        Assert.NotNull(result);
        Assert.Equal(BookingStatus.Cancelled, result!.Status);
        Assert.NotNull(updatedBooking);
        Assert.Equal(BookingStatus.Cancelled, updatedBooking!.Status);

        Assert.NotNull(publishedMessage);
        Assert.Equal(booking.Id, publishedMessage!.BookingId);
        Assert.Equal(booking.EventId, publishedMessage.EventId);
        Assert.Equal(booking.UserId, publishedMessage.UserId);
        Assert.Equal(1, publishedMessage.SeatsCount);
        Assert.Equal(updatedBooking.ProcessedAt, publishedMessage.CancelledAtUtc);
    }

    /// <summary>
    /// Проверяет, что Pending-бронь нельзя отменить до ответа Events, поэтому BookingCancelled не публикуется.
    /// </summary>
    [Fact]
    public async Task CancelBookingAsync_WhenBookingIsPending_ThrowsAndDoesNotPublish()
    {
        var booking = CreateBooking(BookingStatus.Pending);
        var (service, repository, publisher) = CreateService();

        repository.Setup(x => x.GetByIdAsync(booking.Id)).ReturnsAsync(booking);

        await Assert.ThrowsAsync<BookingValidationException>(
            () => service.CancelBookingAsync(booking.Id, booking.UserId, isAdmin: false));

        repository.Verify(x => x.UpdateAsync(It.IsAny<BookingEntity>()), Times.Never);
        publisher.Verify(x => x.PublishBookingCancelledAsync(It.IsAny<BookingCancelled>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private (BookingService service, Mock<IBookingRepository> repository, Mock<IBookingEventPublisher> publisher) CreateService()
    {
        var repository = new Mock<IBookingRepository>();
        var publisher = new Mock<IBookingEventPublisher>();

        repository.Setup(x => x.HasReachedActiveBookingsLimitAsync(It.IsAny<Guid>())).ReturnsAsync(false);
        repository.Setup(x => x.GetActiveBookingsLimit()).Returns(10);
        repository.Setup(x => x.AddAsync(It.IsAny<BookingEntity>())).Returns(Task.CompletedTask);
        repository.Setup(x => x.UpdateAsync(It.IsAny<BookingEntity>())).Returns(Task.CompletedTask);

        publisher.Setup(x => x.PublishBookingRequestedAsync(It.IsAny<BookingRequested>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        publisher.Setup(x => x.PublishBookingCancelledAsync(It.IsAny<BookingCancelled>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return (new BookingService(_mapper, repository.Object, publisher.Object), repository, publisher);
    }

    private static BookingEntity CreateBooking(BookingStatus status)
    {
        return new BookingEntity
        {
            Id = Guid.NewGuid(),
            EventId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Status = status,
            CreatedAt = DateTime.UtcNow.AddMinutes(-10),
            ProcessedAt = status == BookingStatus.Pending ? null : DateTime.UtcNow.AddMinutes(-5)
        };
    }
}

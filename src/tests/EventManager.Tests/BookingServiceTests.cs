using Application.Exceptions;
using Application.Interfaces;
using Application.Models;
using Application.Services;
using AutoMapper;
using Domain.Enums;
using Domain.Exceptions;
using Domain.Models;
using EventManager.Application.Interfaces;
using Moq;

namespace EventManager.Tests;

/// <summary>
/// Набор тестов для проверки бизнес-логики BookingService.
/// </summary>
public class BookingServiceTests
{
    private readonly IMapper _mapper;

    public BookingServiceTests()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<BookingEntity, BookingDto>().ReverseMap();
        });

        _mapper = config.CreateMapper();
    }

    /// <summary>
    /// Проверяет, что успешное создание брони сохраняет идентификатор пользователя и статус Pending.
    /// </summary>
    [Fact]
    public async Task CreateBookingAsync_ValidEvent_ReturnsPendingBookingWithUserId()
    {
        var (service, bookingRepository, eventService, _) = CreateService();
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var eventDto = CreateFutureEvent(eventId);
        BookingEntity? savedBooking = null;

        eventService.Setup(x => x.GetById(eventId)).ReturnsAsync(eventDto);
        eventService.Setup(x => x.UpdateInternal(eventId, It.IsAny<EventDto>()))
            .ReturnsAsync((Guid _, EventDto dto) => dto);
        bookingRepository.Setup(x => x.AddAsync(It.IsAny<BookingEntity>()))
            .Callback<BookingEntity>(booking => savedBooking = booking)
            .Returns(Task.CompletedTask);

        var result = await service.CreateBookingAsync(eventId, userId);

        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result!.Id);
        Assert.Equal(eventId, result.EventId);
        Assert.Equal(userId, result.UserId);
        Assert.Equal(BookingStatus.Pending, result.Status);
        Assert.NotNull(savedBooking);
        Assert.Equal(userId, savedBooking!.UserId);
        bookingRepository.Verify(x => x.AddAsync(It.IsAny<BookingEntity>()), Times.Once);
    }

    /// <summary>
    /// Проверяет, что при создании брони количество свободных мест уменьшается на одно.
    /// </summary>
    [Fact]
    public async Task CreateBookingAsync_ValidEvent_DecreasesAvailableSeatsByOne()
    {
        var (service, _, eventService, _) = CreateService();
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var initialSeats = 10;
        var eventDto = CreateFutureEvent(eventId, initialSeats);
        EventDto? updatedEvent = null;

        eventService.Setup(x => x.GetById(eventId)).ReturnsAsync(eventDto);
        eventService.Setup(x => x.UpdateInternal(eventId, It.IsAny<EventDto>()))
            .ReturnsAsync((Guid _, EventDto dto) =>
            {
                updatedEvent = dto;
                return dto;
            });

        await service.CreateBookingAsync(eventId, userId);

        Assert.NotNull(updatedEvent);
        Assert.Equal(initialSeats - 1, updatedEvent!.AvailableSeats);
    }

    /// <summary>
    /// Проверяет, что несколько броней одного события получают уникальные идентификаторы.
    /// </summary>
    [Fact]
    public async Task CreateBookingAsync_MultipleBookingsForSameEvent_ReturnsUniqueIds()
    {
        var (service, bookingRepository, eventService, _) = CreateService();
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var currentSeats = 5;

        eventService.Setup(x => x.GetById(eventId))
            .ReturnsAsync(() => CreateFutureEvent(eventId, currentSeats));
        eventService.Setup(x => x.UpdateInternal(eventId, It.IsAny<EventDto>()))
            .ReturnsAsync((Guid _, EventDto dto) =>
            {
                currentSeats = dto.AvailableSeats;
                return dto;
            });

        var firstBooking = await service.CreateBookingAsync(eventId, userId);
        var secondBooking = await service.CreateBookingAsync(eventId, userId);
        var thirdBooking = await service.CreateBookingAsync(eventId, userId);

        Assert.NotNull(firstBooking);
        Assert.NotNull(secondBooking);
        Assert.NotNull(thirdBooking);
        Assert.Equal(3, new[] { firstBooking!.Id, secondBooking!.Id, thirdBooking!.Id }.Distinct().Count());
        bookingRepository.Verify(x => x.AddAsync(It.IsAny<BookingEntity>()), Times.Exactly(3));
    }

    /// <summary>
    /// Проверяет, что для несуществующего события бронь не создается.
    /// </summary>
    [Fact]
    public async Task CreateBookingAsync_WhenEventDoesNotExist_ThrowsNotFoundException()
    {
        var (service, bookingRepository, eventService, _) = CreateService();
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        eventService.Setup(x => x.GetById(eventId)).ReturnsAsync((EventDto?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => service.CreateBookingAsync(eventId, userId));
        bookingRepository.Verify(x => x.AddAsync(It.IsAny<BookingEntity>()), Times.Never);
    }

    /// <summary>
    /// Проверяет запрет бронирования события, которое уже началось.
    /// </summary>
    [Fact]
    public async Task CreateBookingAsync_WhenEventHasAlreadyStarted_ThrowsPastEventBookingException()
    {
        var (service, bookingRepository, eventService, _) = CreateService();
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var eventDto = CreateFutureEvent(eventId);
        eventDto.StartDate = DateTime.UtcNow.AddMinutes(-1);
        eventDto.EndDate = DateTime.UtcNow.AddHours(1);

        eventService.Setup(x => x.GetById(eventId)).ReturnsAsync(eventDto);

        await Assert.ThrowsAsync<PastEventBookingException>(() => service.CreateBookingAsync(eventId, userId));
        bookingRepository.Verify(x => x.AddAsync(It.IsAny<BookingEntity>()), Times.Never);
        eventService.Verify(x => x.UpdateInternal(It.IsAny<Guid>(), It.IsAny<EventDto>()), Times.Never);
    }

    /// <summary>
    /// Проверяет, что бронь не создается для несуществующего пользователя.
    /// </summary>
    [Fact]
    public async Task CreateBookingAsync_WhenUserDoesNotExist_ThrowsNotFoundException()
    {
        var (service, bookingRepository, eventService, userService) = CreateService();
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        eventService.Setup(x => x.GetById(eventId)).ReturnsAsync(CreateFutureEvent(eventId));
        userService.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync((UserEntity?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => service.CreateBookingAsync(eventId, userId));
        bookingRepository.Verify(x => x.AddAsync(It.IsAny<BookingEntity>()), Times.Never);
    }

    /// <summary>
    /// Проверяет, что при отсутствии свободных мест возвращается доменная ошибка.
    /// </summary>
    [Fact]
    public async Task CreateBookingAsync_WhenNoAvailableSeats_ThrowsNoAvailableSeatsException()
    {
        var (service, bookingRepository, eventService, _) = CreateService();
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        eventService.Setup(x => x.GetById(eventId)).ReturnsAsync(CreateFutureEvent(eventId, availableSeats: 0));

        await Assert.ThrowsAsync<NoAvailableSeatsException>(() => service.CreateBookingAsync(eventId, userId));
        bookingRepository.Verify(x => x.AddAsync(It.IsAny<BookingEntity>()), Times.Never);
    }

    /// <summary>
    /// Проверяет, что при достижении лимита активных броней новая бронь не создается.
    /// </summary>
    [Fact]
    public async Task CreateBookingAsync_WhenActiveBookingLimitReached_ThrowsActiveBookingsLimitExceededException()
    {
        var (service, bookingRepository, eventService, _) = CreateService();
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        eventService.Setup(x => x.GetById(eventId)).ReturnsAsync(CreateFutureEvent(eventId));
        bookingRepository.Setup(x => x.HasReachedActiveBookingsLimitAsync(userId)).ReturnsAsync(true);

        await Assert.ThrowsAsync<ActiveBookingsLimitExceededException>(() => service.CreateBookingAsync(eventId, userId));
        bookingRepository.Verify(x => x.AddAsync(It.IsAny<BookingEntity>()), Times.Never);
        eventService.Verify(x => x.UpdateInternal(It.IsAny<Guid>(), It.IsAny<EventDto>()), Times.Never);
    }

    /// <summary>
    /// Проверяет, что лимит активных броней применяется отдельно для каждого пользователя.
    /// </summary>
    [Fact]
    public async Task CreateBookingAsync_LimitsForDifferentUsers_DoNotAffectEachOther()
    {
        var (service, bookingRepository, eventService, _) = CreateService();
        var eventId = Guid.NewGuid();
        var limitedUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var eventDto = CreateFutureEvent(eventId, availableSeats: 2);

        eventService.Setup(x => x.GetById(eventId)).ReturnsAsync(eventDto);
        eventService.Setup(x => x.UpdateInternal(eventId, It.IsAny<EventDto>()))
            .ReturnsAsync((Guid _, EventDto dto) => dto);
        bookingRepository.Setup(x => x.HasReachedActiveBookingsLimitAsync(limitedUserId)).ReturnsAsync(true);
        bookingRepository.Setup(x => x.HasReachedActiveBookingsLimitAsync(otherUserId)).ReturnsAsync(false);

        await Assert.ThrowsAsync<ActiveBookingsLimitExceededException>(
            () => service.CreateBookingAsync(eventId, limitedUserId));
        var result = await service.CreateBookingAsync(eventId, otherUserId);

        Assert.NotNull(result);
        Assert.Equal(otherUserId, result!.UserId);
        bookingRepository.Verify(x => x.AddAsync(It.IsAny<BookingEntity>()), Times.Once);
    }

    /// <summary>
    /// Проверяет, что пользователь может получить свою бронь.
    /// </summary>
    [Fact]
    public async Task GetBookingByIdAsync_Owner_ReturnsBooking()
    {
        var (service, bookingRepository, _, _) = CreateService();
        var userId = Guid.NewGuid();
        var booking = CreateBooking(userId);

        bookingRepository.Setup(x => x.GetByIdAsync(booking.Id)).ReturnsAsync(booking);

        var result = await service.GetBookingByIdAsync(booking.Id, userId);

        Assert.NotNull(result);
        Assert.Equal(booking.Id, result!.Id);
        Assert.Equal(userId, result.UserId);
    }

    /// <summary>
    /// Проверяет, что администратор может получить бронь любого пользователя.
    /// </summary>
    [Fact]
    public async Task GetBookingByIdAsync_Admin_ReturnsOtherUsersBooking()
    {
        var (service, bookingRepository, _, userService) = CreateService();
        var ownerId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var booking = CreateBooking(ownerId);

        bookingRepository.Setup(x => x.GetByIdAsync(booking.Id)).ReturnsAsync(booking);
        userService.Setup(x => x.GetByIdAsync(adminId)).ReturnsAsync(CreateUser(adminId, Roles.Admin));

        var result = await service.GetBookingByIdAsync(booking.Id, adminId);

        Assert.NotNull(result);
        Assert.Equal(ownerId, result!.UserId);
    }

    /// <summary>
    /// Проверяет запрет просмотра чужой брони обычным пользователем.
    /// </summary>
    [Fact]
    public async Task GetBookingByIdAsync_NotOwner_ThrowsOperationForbiddenException()
    {
        var (service, bookingRepository, _, _) = CreateService();
        var ownerId = Guid.NewGuid();
        var anotherUserId = Guid.NewGuid();
        var booking = CreateBooking(ownerId);

        bookingRepository.Setup(x => x.GetByIdAsync(booking.Id)).ReturnsAsync(booking);

        await Assert.ThrowsAsync<OperationForbiddenException>(
            () => service.GetBookingByIdAsync(booking.Id, anotherUserId));
    }

    /// <summary>
    /// Проверяет, что для несуществующей брони сервис возвращает null.
    /// </summary>
    [Fact]
    public async Task GetBookingByIdAsync_WhenBookingDoesNotExist_ReturnsNull()
    {
        var (service, bookingRepository, _, _) = CreateService();
        var bookingId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        bookingRepository.Setup(x => x.GetByIdAsync(bookingId)).ReturnsAsync((BookingEntity?)null);

        var result = await service.GetBookingByIdAsync(bookingId, userId);

        Assert.Null(result);
    }

    /// <summary>
    /// Проверяет, что владелец может отменить бронь, а место возвращается в событие.
    /// </summary>
    [Fact]
    public async Task CancelBookingAsync_Owner_CancelsBookingAndReleasesSeat()
    {
        var (service, bookingRepository, eventService, _) = CreateService();
        var userId = Guid.NewGuid();
        var booking = CreateBooking(userId);
        var eventDto = CreateFutureEvent(booking.EventId, availableSeats: 4);
        eventDto.TotalSeats = 5;
        BookingEntity? updatedBooking = null;

        bookingRepository.Setup(x => x.GetByIdAsync(booking.Id)).ReturnsAsync(booking);
        bookingRepository.Setup(x => x.UpdateAsync(It.IsAny<BookingEntity>()))
            .Callback<BookingEntity>(value => updatedBooking = value)
            .Returns(Task.CompletedTask);
        eventService.Setup(x => x.GetById(booking.EventId)).ReturnsAsync(eventDto);
        eventService.Setup(x => x.UpdateInternal(booking.EventId, It.IsAny<EventDto>()))
            .ReturnsAsync((Guid _, EventDto dto) => dto);

        var result = await service.CancelBookingAsync(booking.Id, userId);

        Assert.NotNull(result);
        Assert.Equal(BookingStatus.Cancelled, result!.Status);
        Assert.NotNull(updatedBooking);
        Assert.Equal(BookingStatus.Cancelled, updatedBooking!.Status);
        Assert.Equal(5, eventDto.AvailableSeats);
    }

    /// <summary>
    /// Проверяет, что администратор может отменить бронь другого пользователя.
    /// </summary>
    [Fact]
    public async Task CancelBookingAsync_Admin_CancelsOtherUsersBooking()
    {
        var (service, bookingRepository, eventService, userService) = CreateService();
        var ownerId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var booking = CreateBooking(ownerId);

        bookingRepository.Setup(x => x.GetByIdAsync(booking.Id)).ReturnsAsync(booking);
        bookingRepository.Setup(x => x.UpdateAsync(It.IsAny<BookingEntity>())).Returns(Task.CompletedTask);
        eventService.Setup(x => x.GetById(booking.EventId)).ReturnsAsync(CreateFutureEvent(booking.EventId));
        eventService.Setup(x => x.UpdateInternal(booking.EventId, It.IsAny<EventDto>()))
            .ReturnsAsync((Guid _, EventDto dto) => dto);
        userService.Setup(x => x.GetByIdAsync(adminId)).ReturnsAsync(CreateUser(adminId, Roles.Admin));

        var result = await service.CancelBookingAsync(booking.Id, adminId);

        Assert.NotNull(result);
        Assert.Equal(BookingStatus.Cancelled, result!.Status);
        bookingRepository.Verify(x => x.UpdateAsync(It.IsAny<BookingEntity>()), Times.Once);
    }

    /// <summary>
    /// Проверяет запрет отмены чужой брони обычным пользователем.
    /// </summary>
    [Fact]
    public async Task CancelBookingAsync_NotOwner_ThrowsOperationForbiddenException()
    {
        var (service, bookingRepository, _, _) = CreateService();
        var ownerId = Guid.NewGuid();
        var anotherUserId = Guid.NewGuid();
        var booking = CreateBooking(ownerId);

        bookingRepository.Setup(x => x.GetByIdAsync(booking.Id)).ReturnsAsync(booking);

        await Assert.ThrowsAsync<OperationForbiddenException>(
            () => service.CancelBookingAsync(booking.Id, anotherUserId));
        bookingRepository.Verify(x => x.UpdateAsync(It.IsAny<BookingEntity>()), Times.Never);
    }

    /// <summary>
    /// Проверяет защиту от повторной отмены брони.
    /// </summary>
    [Fact]
    public async Task CancelBookingAsync_AlreadyCancelled_ReturnsBookingWithoutUpdate()
    {
        var (service, bookingRepository, eventService, _) = CreateService();
        var userId = Guid.NewGuid();
        var booking = CreateBooking(userId, BookingStatus.Cancelled);

        bookingRepository.Setup(x => x.GetByIdAsync(booking.Id)).ReturnsAsync(booking);

        var result = await service.CancelBookingAsync(booking.Id, userId);

        Assert.NotNull(result);
        Assert.Equal(BookingStatus.Cancelled, result!.Status);
        bookingRepository.Verify(x => x.UpdateAsync(It.IsAny<BookingEntity>()), Times.Never);
        eventService.Verify(x => x.UpdateInternal(It.IsAny<Guid>(), It.IsAny<EventDto>()), Times.Never);
    }

    private (BookingService service,
        Mock<IBookingRepository> bookingRepository,
        Mock<IEventService> eventService,
        Mock<IUserService> userService) CreateService()
    {
        var bookingRepository = new Mock<IBookingRepository>();
        var eventService = new Mock<IEventService>();
        var userService = new Mock<IUserService>();

        bookingRepository.Setup(x => x.HasReachedActiveBookingsLimitAsync(It.IsAny<Guid>()))
            .ReturnsAsync(false);
        bookingRepository.Setup(x => x.GetActiveBookingsLimit()).Returns(10);
        bookingRepository.Setup(x => x.AddAsync(It.IsAny<BookingEntity>()))
            .Returns(Task.CompletedTask);
        userService.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid id) => CreateUser(id, Roles.User));

        var service = new BookingService(_mapper, eventService.Object, bookingRepository.Object, userService.Object);
        return (service, bookingRepository, eventService, userService);
    }

    private static EventDto CreateFutureEvent(Guid eventId, int availableSeats = 5)
    {
        return new EventDto
        {
            Id = eventId,
            Title = "Test Event",
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(1).AddHours(2),
            TotalSeats = Math.Max(availableSeats, 1),
            AvailableSeats = availableSeats
        };
    }

    private static BookingEntity CreateBooking(Guid userId, BookingStatus status = BookingStatus.Pending)
    {
        return new BookingEntity
        {
            Id = Guid.NewGuid(),
            EventId = Guid.NewGuid(),
            UserId = userId,
            Status = status,
            CreatedAt = DateTime.UtcNow.AddMinutes(-5),
            ProcessedAt = status == BookingStatus.Pending ? null : DateTime.UtcNow
        };
    }

    private static UserEntity CreateUser(Guid id, Roles role)
    {
        return new UserEntity
        {
            Id = id,
            Login = $"user-{id:N}",
            PasswordHash = "hash",
            Role = role
        };
    }
}

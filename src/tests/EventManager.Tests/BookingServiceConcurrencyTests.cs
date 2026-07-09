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
/// Набор тестов для проверки конкурентного создания броней.
/// </summary>
public class BookingServiceConcurrencyTests
{
    private readonly IMapper _mapper;

    public BookingServiceConcurrencyTests()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<BookingEntity, BookingDto>().ReverseMap();
        });

        _mapper = config.CreateMapper();
    }

    /// <summary>
    /// Проверяет, что параллельные запросы не могут создать броней больше, чем свободных мест.
    /// </summary>
    [Fact]
    public async Task CreateBookingAsync_ConcurrentRequests_PreventsOverbooking()
    {
        var totalSeats = 5;
        var concurrentRequests = 20;
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var currentSeats = totalSeats;
        var (service, _, eventService, _) = CreateService();
        var exceptions = new List<Exception>();
        var successfulBookings = new List<BookingDto>();
        var lockObject = new object();

        eventService.Setup(x => x.GetById(eventId))
            .ReturnsAsync(() => CreateFutureEvent(eventId, totalSeats, currentSeats));
        eventService.Setup(x => x.UpdateInternal(eventId, It.IsAny<EventDto>()))
            .ReturnsAsync((Guid _, EventDto dto) =>
            {
                Thread.Sleep(10);
                currentSeats = dto.AvailableSeats;
                return dto;
            });

        var tasks = Enumerable.Range(0, concurrentRequests)
            .Select(_ => Task.Run(async () =>
            {
                try
                {
                    var result = await service.CreateBookingAsync(eventId, userId);
                    lock (lockObject)
                    {
                        if (result != null)
                        {
                            successfulBookings.Add(result);
                        }
                    }
                }
                catch (Exception ex)
                {
                    lock (lockObject)
                    {
                        exceptions.Add(ex);
                    }
                }
            }))
            .ToList();

        await Task.WhenAll(tasks);

        Assert.Equal(totalSeats, successfulBookings.Count);
        Assert.Equal(concurrentRequests - totalSeats, exceptions.Count);
        Assert.All(exceptions, ex => Assert.IsType<NoAvailableSeatsException>(ex));
        Assert.Equal(0, currentSeats);
        Assert.Equal(totalSeats, successfulBookings.Select(b => b.Id).Distinct().Count());
        Assert.All(successfulBookings, b => Assert.Equal(userId, b.UserId));
    }

    /// <summary>
    /// Проверяет, что параллельно созданные брони получают уникальные идентификаторы.
    /// </summary>
    [Fact]
    public async Task CreateBookingAsync_ConcurrentRequests_AllBookingsHaveUniqueIds()
    {
        var totalSeats = 10;
        var concurrentRequests = 10;
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var currentSeats = totalSeats;
        var (service, _, eventService, _) = CreateService();
        var successfulBookings = new List<BookingDto>();
        var lockObject = new object();

        eventService.Setup(x => x.GetById(eventId))
            .ReturnsAsync(() => CreateFutureEvent(eventId, totalSeats, currentSeats));
        eventService.Setup(x => x.UpdateInternal(eventId, It.IsAny<EventDto>()))
            .ReturnsAsync((Guid _, EventDto dto) =>
            {
                Thread.Sleep(5);
                currentSeats = dto.AvailableSeats;
                return dto;
            });

        var tasks = Enumerable.Range(0, concurrentRequests)
            .Select(_ => Task.Run(async () =>
            {
                var result = await service.CreateBookingAsync(eventId, userId);
                lock (lockObject)
                {
                    if (result != null)
                    {
                        successfulBookings.Add(result);
                    }
                }
            }))
            .ToList();

        await Task.WhenAll(tasks);

        Assert.Equal(concurrentRequests, successfulBookings.Count);
        Assert.Equal(concurrentRequests, successfulBookings.Select(b => b.Id).Distinct().Count());
        Assert.All(successfulBookings, b => Assert.Equal(eventId, b.EventId));
        Assert.All(successfulBookings, b => Assert.Equal(userId, b.UserId));
        Assert.All(successfulBookings, b => Assert.Equal(BookingStatus.Pending, b.Status));
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
            .ReturnsAsync((Guid id) => new UserEntity
            {
                Id = id,
                Login = $"user-{id:N}",
                PasswordHash = "hash",
                Role = Roles.User
            });

        var service = new BookingService(_mapper, eventService.Object, bookingRepository.Object, userService.Object);
        return (service, bookingRepository, eventService, userService);
    }

    private static EventDto CreateFutureEvent(Guid eventId, int totalSeats, int availableSeats)
    {
        return new EventDto
        {
            Id = eventId,
            Title = "Test Event",
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(1).AddHours(2),
            TotalSeats = totalSeats,
            AvailableSeats = availableSeats
        };
    }
}

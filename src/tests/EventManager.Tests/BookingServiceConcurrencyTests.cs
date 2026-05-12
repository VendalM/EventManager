using AutoMapper;
using EventManager.Application.Interfaces;
using EventManager.Application.Services;
using EventManager.Enums;
using EventManager.Models;
using EventManager.Exceptions;
using Moq;

namespace EventManager.Tests;

/// <summary>
/// Набор тестов для проверки конкурентности при бронировании
/// </summary>
public class BookingServiceConcurrencyTests
{
    private readonly IMapper _mapper;
    
    public BookingServiceConcurrencyTests()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<BookingEntity, BookingDto>();
        });
        _mapper = config.CreateMapper();
    }
    
    private (BookingService service, Mock<IBookingRepository> mockRepo, Mock<IEventService> mockEvent) 
        CreateBookingService()
    {
        var mockRepo = new Mock<IBookingRepository>();
        var mockEvent = new Mock<IEventService>();
        var service = new BookingService(_mapper, mockEvent.Object, mockRepo.Object);
        
        return (service, mockRepo, mockEvent);
    }
    
    /// <summary>
    /// Тест на защиту от овербукинга:
    /// Дано: событие на 5 мест, 20 конкурентных запросов.
    /// Ожидается: ровно 5 успешных броней, 15 — NoAvailableSeatsException,
    /// AvailableSeats = 0.
    /// </summary>
    [Fact]
    public async Task CreateBookingAsync_ConcurrentRequests_PreventsOverbooking()
    {
        // Arrange
        var totalSeats = 5;
        var concurrentRequests = 20;
        var eventId = Guid.NewGuid();
        var currentSeats = totalSeats;
        
        var (service, mockRepo, mockEvent) = CreateBookingService();
        
        // Настраиваем моки с задержкой для имитации реальной конкурентности
        mockEvent.Setup(x => x.HasEvent(eventId)).ReturnsAsync(true);
        
        mockEvent.Setup(x => x.GetById(eventId))
            .ReturnsAsync(() => new EventDto 
            { 
                Id = eventId, 
                Title = "Test Event",
                TotalSeats = totalSeats,
                AvailableSeats = currentSeats 
            });
        
        mockEvent.Setup(x => x.UpdateInternal(eventId, It.IsAny<EventDto>()))
            .ReturnsAsync((Guid id, EventDto dto) => 
            {
                // Имитируем задержку при обновлении
                Thread.Sleep(10);
                currentSeats = dto.AvailableSeats;
                return dto;
            });
        
        mockRepo.Setup(x => x.AddAsync(It.IsAny<BookingEntity>()))
            .Returns(Task.CompletedTask)
            .Callback<BookingEntity>(booking => 
            {
                booking.Id = Guid.NewGuid();
            });
        
        // Act - запускаем конкурентные запросы
        var tasks = new List<Task<BookingDto?>>();
        var exceptions = new List<Exception>();
        var successfulBookings = new List<BookingDto>();
        var lockObject = new object();
        
        for (int i = 0; i < concurrentRequests; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    var result = await service.CreateBookingAsync(eventId);
                    lock (lockObject)
                    {
                        if (result != null)
                            successfulBookings.Add(result);
                    }
                    return result;
                }
                catch (Exception ex)
                {
                    lock (lockObject)
                    {
                        exceptions.Add(ex);
                    }
                    return null;
                }
            }));
        }
        
        // Ждём завершения всех задач
        await Task.WhenAll(tasks);
        
        // Assert
        Assert.Equal(totalSeats, successfulBookings.Count);
        Assert.Equal(concurrentRequests - totalSeats, exceptions.Count);
        Assert.All(exceptions, ex => Assert.IsType<NoAvailableSeatsException>(ex));
        Assert.Equal(0, currentSeats);
        
        var uniqueIds = successfulBookings.Select(b => b.Id).Distinct().Count();
        Assert.Equal(totalSeats, uniqueIds);
    }
    
    /// <summary>
    /// Тест на уникальность Id при конкурентных запросах:
    /// Дано: событие на 10 мест, 10 одновременных запросов.
    /// Ожидается: 10 броней с уникальными Id.
    /// </summary>
    [Fact]
    public async Task CreateBookingAsync_ConcurrentRequests_AllBookingsHaveUniqueIds()
    {
        // Arrange
        var totalSeats = 10;
        var concurrentRequests = 10;
        var eventId = Guid.NewGuid();
        var currentSeats = totalSeats;
        
        var (service, mockRepo, mockEvent) = CreateBookingService();
        
        mockEvent.Setup(x => x.HasEvent(eventId)).ReturnsAsync(true);
        
        mockEvent.Setup(x => x.GetById(eventId))
            .ReturnsAsync(() => new EventDto 
            { 
                Id = eventId, 
                Title = "Test Event",
                TotalSeats = totalSeats,
                AvailableSeats = currentSeats 
            });
        
        mockEvent.Setup(x => x.UpdateInternal(eventId, It.IsAny<EventDto>()))
            .ReturnsAsync((Guid id, EventDto dto) => 
            {
                Thread.Sleep(5);
                currentSeats = dto.AvailableSeats;
                return dto;
            });
        
        var usedIds = new HashSet<Guid>();
        mockRepo.Setup(x => x.AddAsync(It.IsAny<BookingEntity>()))
            .Returns(Task.CompletedTask)
            .Callback<BookingEntity>(booking => 
            {
                // Генерируем уникальный Id
                var newId = Guid.NewGuid();
                while (usedIds.Contains(newId))
                {
                    newId = Guid.NewGuid();
                }
                usedIds.Add(newId);
                booking.Id = newId;
            });
        
        // Act
        var tasks = new List<Task<BookingDto?>>();
        var successfulBookings = new List<BookingDto>();
        var lockObject = new object();
        
        for (int i = 0; i < concurrentRequests; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                var result = await service.CreateBookingAsync(eventId);
                lock (lockObject)
                {
                    if (result != null)
                        successfulBookings.Add(result);
                }
                return result;
            }));
        }
        
        await Task.WhenAll(tasks);
        
        // Assert
        Assert.Equal(concurrentRequests, successfulBookings.Count);
        
        var uniqueIds = successfulBookings.Select(b => b.Id).Distinct().Count();
        Assert.Equal(concurrentRequests, uniqueIds);
        
        Assert.All(successfulBookings, b => Assert.Equal(eventId, b.EventId));
        Assert.All(successfulBookings, b => Assert.Equal(BookingStatus.Pending, b.Status));
    }
}
using AutoMapper;
using EventManager.Application.Interfaces;
using EventManager.Application.Services;
using EventManager.Enums;
using EventManager.Models;
using EventManager.Exceptions;
using Moq;

namespace EventManager.Tests;

/// <summary>
/// Набор тестов для проверки функциональности BookingService
/// </summary>
public class BookingServiceTests
{
    private readonly IMapper _mapper;
    
    public BookingServiceTests()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<BookingEntity, BookingDto>();
            cfg.CreateMap<EventDto, EventDto>(); // Для UpdateInternal
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

    #region Успешные сценарии

    /// <summary>
    /// Проверка создания брони для существующего события - возвращается BookingDto со статусом Pending
    /// </summary>
    [Fact]
    public async Task CreateBookingAsync_ValidEventId_ReturnsBookingWithPendingStatus()
    {
        // Arrange
        var (service, mockRepo, mockEvent) = CreateBookingService();
        var eventId = Guid.NewGuid();
        var eventDto = new EventDto 
        { 
            Id = eventId, 
            Title = "Test Event",
            AvailableSeats = 5 
        };
        
        mockEvent.Setup(x => x.HasEvent(eventId)).ReturnsAsync(true);
        mockEvent.Setup(x => x.GetById(eventId)).ReturnsAsync(eventDto);
        mockEvent.Setup(x => x.UpdateInternal(eventId, It.IsAny<EventDto>()))
            .ReturnsAsync((Guid id, EventDto dto) => dto);
        
        // Act
        var result = await service.CreateBookingAsync(eventId);
        
        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(eventId, result.EventId);
        Assert.Equal(BookingStatus.Pending, result.Status);
        mockRepo.Verify(x => x.AddAsync(It.IsAny<BookingEntity>()), Times.Once);
    }
    
    /// <summary>
    /// Создание брони уменьшает AvailableSeats на 1
    /// </summary>
    [Fact]
    public async Task CreateBookingAsync_ValidEvent_DecreasesAvailableSeatsByOne()
    {
        // Arrange
        var (service, mockRepo, mockEvent) = CreateBookingService();
        var eventId = Guid.NewGuid();
        var initialSeats = 10;
        var eventDto = new EventDto 
        { 
            Id = eventId, 
            Title = "Test Event",
            AvailableSeats = initialSeats 
        };
        
        mockEvent.Setup(x => x.HasEvent(eventId)).ReturnsAsync(true);
        mockEvent.Setup(x => x.GetById(eventId)).ReturnsAsync(eventDto);
        
        EventDto? updatedEventDto = null;
        mockEvent.Setup(x => x.UpdateInternal(eventId, It.IsAny<EventDto>()))
            .ReturnsAsync((Guid id, EventDto dto) => 
            {
                updatedEventDto = dto;
                return dto;
            });
        
        // Act
        await service.CreateBookingAsync(eventId);
        
        // Assert
        Assert.NotNull(updatedEventDto);
        Assert.Equal(initialSeats - 1, updatedEventDto.AvailableSeats);
    }
    
    /// <summary>
    /// Проверка создания нескольких броней для одного события - все создаются с уникальными Id
    /// </summary>
    [Fact]
    public async Task CreateBookingAsync_MultipleBookingsForSameEvent_ReturnsUniqueIds()
    {
        // Arrange
        var (service, mockRepo, mockEvent) = CreateBookingService();
        var eventId = Guid.NewGuid();
        var totalSeats = 5;
        var currentSeats = totalSeats;
        
        mockEvent.Setup(x => x.HasEvent(eventId)).ReturnsAsync(true);
        mockEvent.Setup(x => x.GetById(eventId))
            .ReturnsAsync(() => new EventDto 
            { 
                Id = eventId, 
                Title = "Test Event",
                AvailableSeats = currentSeats 
            });
        
        mockEvent.Setup(x => x.UpdateInternal(eventId, It.IsAny<EventDto>()))
            .ReturnsAsync((Guid id, EventDto dto) => 
            {
                currentSeats = dto.AvailableSeats;
                return dto;
            });
        
        // Act
        var booking1 = await service.CreateBookingAsync(eventId);
        var booking2 = await service.CreateBookingAsync(eventId);
        var booking3 = await service.CreateBookingAsync(eventId);
        
        // Assert
        Assert.NotNull(booking1);
        Assert.NotNull(booking2);
        Assert.NotNull(booking3);
        
        Assert.NotEqual(booking1.Id, booking2.Id);
        Assert.NotEqual(booking1.Id, booking3.Id);
        Assert.NotEqual(booking2.Id, booking3.Id);
        
        mockRepo.Verify(x => x.AddAsync(It.IsAny<BookingEntity>()), Times.Exactly(3));
    }
    
    /// <summary>
    /// Создание нескольких броней до лимита — все успешны
    /// </summary>
    [Fact]
    public async Task CreateBookingAsync_BookingsUntilLimit_AllSucceed()
    {
        // Arrange
        var (service, mockRepo, mockEvent) = CreateBookingService();
        var eventId = Guid.NewGuid();
        var totalSeats = 3;
        var currentSeats = totalSeats;
        
        mockEvent.Setup(x => x.HasEvent(eventId)).ReturnsAsync(true);
        mockEvent.Setup(x => x.GetById(eventId))
            .ReturnsAsync(() => new EventDto 
            { 
                Id = eventId, 
                Title = "Test Event",
                AvailableSeats = currentSeats 
            });
        
        mockEvent.Setup(x => x.UpdateInternal(eventId, It.IsAny<EventDto>()))
            .ReturnsAsync((Guid id, EventDto dto) => 
            {
                currentSeats = dto.AvailableSeats;
                return dto;
            });
        
        // Act
        var bookings = new List<BookingDto>();
        for (int i = 0; i < totalSeats; i++)
        {
            var booking = await service.CreateBookingAsync(eventId);
            bookings.Add(booking);
        }
        
        // Assert
        Assert.Equal(totalSeats, bookings.Count);
        Assert.All(bookings, b => Assert.NotNull(b));
        Assert.Equal(totalSeats, bookings.Select(b => b.Id).Distinct().Count());
        Assert.Equal(0, currentSeats);
        
        mockRepo.Verify(x => x.AddAsync(It.IsAny<BookingEntity>()), Times.Exactly(totalSeats));
        mockEvent.Verify(x => x.UpdateInternal(eventId, It.IsAny<EventDto>()), 
            Times.Exactly(totalSeats));
    }
    
    /// <summary>
    /// Проверка получения брони по Id - возвращается корректная информация
    /// </summary>
    [Fact]
    public async Task GetBookingByIdAsync_ValidId_ReturnsCorrectBooking()
    {
        // Arrange
        var (service, mockRepo, mockEvent) = CreateBookingService();
        var bookingId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;
        
        var expectedBooking = new BookingEntity
        {
            Id = bookingId,
            EventId = eventId,
            Status = BookingStatus.Pending,
            CreatedAt = createdAt,
            ProcessedAt = null
        };
        
        mockRepo.Setup(x => x.GetByIdAsync(bookingId))
            .ReturnsAsync(expectedBooking);
        
        // Act
        var result = await service.GetBookingByIdAsync(bookingId);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(bookingId, result.Id);
        Assert.Equal(eventId, result.EventId);
        Assert.Equal(BookingStatus.Pending, result.Status);
    }
    
    /// <summary>
    /// Проверка получения брони отражает изменение статуса (после Confirm)
    /// </summary>
    [Fact]
    public async Task GetBookingByIdAsync_AfterStatusChange_ReturnsUpdatedStatus()
    {
        // Arrange
        var (service, mockRepo, mockEvent) = CreateBookingService();
        var bookingId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        
        var booking = new BookingEntity
        {
            Id = bookingId,
            EventId = eventId,
            Status = BookingStatus.Confirmed,
            CreatedAt = DateTime.UtcNow.AddMinutes(-5),
            ProcessedAt = DateTime.UtcNow
        };
        
        mockRepo.Setup(x => x.GetByIdAsync(bookingId))
            .ReturnsAsync(booking);
        
        // Act
        var result = await service.GetBookingByIdAsync(bookingId);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(BookingStatus.Confirmed, result.Status);
        Assert.NotNull(result.ProcessedAt);
    }

    #endregion

    #region Неуспешные сценарии

    /// <summary>
    /// Бронирование для несуществующего события → NotFoundException
    /// </summary>
    [Fact]
    public async Task CreateBookingAsync_NonExistentEvent_ThrowsNotFoundException()
    {
        // Arrange
        var (service, mockRepo, mockEvent) = CreateBookingService();
        var eventId = Guid.NewGuid();
    
        mockEvent.Setup(x => x.HasEvent(eventId)).ReturnsAsync(false);
        mockEvent.Setup(x => x.GetById(eventId)).ReturnsAsync((EventDto?)null);
    
        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => 
            service.CreateBookingAsync(eventId));
    
        mockRepo.Verify(x => x.AddAsync(It.IsAny<BookingEntity>()), Times.Never);
        mockEvent.Verify(x => x.UpdateInternal(It.IsAny<Guid>(), It.IsAny<EventDto>()), 
            Times.Never);
    }
    
    /// <summary>
    /// Бронирование при отсутствии мест → NoAvailableSeatsException
    /// </summary>
    [Fact]
    public async Task CreateBookingAsync_NoAvailableSeats_ThrowsNoAvailableSeatsException()
    {
        // Arrange
        var (service, mockRepo, mockEvent) = CreateBookingService();
        var eventId = Guid.NewGuid();
        var eventDto = new EventDto 
        { 
            Id = eventId, 
            Title = "Test Event",
            AvailableSeats = 0 
        };
        
        mockEvent.Setup(x => x.HasEvent(eventId)).ReturnsAsync(true);
        mockEvent.Setup(x => x.GetById(eventId)).ReturnsAsync(eventDto);
        
        // Act & Assert
        await Assert.ThrowsAsync<NoAvailableSeatsException>(() => 
            service.CreateBookingAsync(eventId));
        
        mockRepo.Verify(x => x.AddAsync(It.IsAny<BookingEntity>()), Times.Never);
        mockEvent.Verify(x => x.UpdateInternal(It.IsAny<Guid>(), It.IsAny<EventDto>()), 
            Times.Never);
    }
    
    /// <summary>
    /// После исчерпания мест следующая попытка выбрасывает NoAvailableSeatsException
    /// </summary>
    [Fact]
    public async Task CreateBookingAsync_AfterSeatsExhausted_ThrowsNoAvailableSeatsException()
    {
        // Arrange
        var (service, mockRepo, mockEvent) = CreateBookingService();
        var eventId = Guid.NewGuid();
        var totalSeats = 2;
        var currentSeats = totalSeats;
        
        mockEvent.Setup(x => x.HasEvent(eventId)).ReturnsAsync(true);
        mockEvent.Setup(x => x.GetById(eventId))
            .ReturnsAsync(() => new EventDto 
            { 
                Id = eventId, 
                Title = "Test Event",
                AvailableSeats = currentSeats 
            });
        
        mockEvent.Setup(x => x.UpdateInternal(eventId, It.IsAny<EventDto>()))
            .ReturnsAsync((Guid id, EventDto dto) => 
            {
                currentSeats = dto.AvailableSeats;
                return dto;
            });
        
        // Act - создаём брони до заполнения всех мест
        for (int i = 0; i < totalSeats; i++)
        {
            await service.CreateBookingAsync(eventId);
        }
        
        // Assert - следующая попытка должна выбросить исключение
        await Assert.ThrowsAsync<NoAvailableSeatsException>(() => 
            service.CreateBookingAsync(eventId));
        
        // Проверяем, что было создано ровно totalSeats броней
        mockRepo.Verify(x => x.AddAsync(It.IsAny<BookingEntity>()), 
            Times.Exactly(totalSeats));
        mockEvent.Verify(x => x.UpdateInternal(eventId, It.IsAny<EventDto>()), 
            Times.Exactly(totalSeats));
    }
    
    /// <summary>
    /// Проверка создания брони для удалённого события
    /// </summary>
    [Fact]
    public async Task CreateBookingAsync_DeletedEvent_ThrowsNotFoundException()
    {
        // Arrange
        var (service, mockRepo, mockEvent) = CreateBookingService();
        var eventId = Guid.NewGuid();
        var eventDto = new EventDto 
        { 
            Id = eventId, 
            Title = "Test Event",
            AvailableSeats = 5 
        };
    
        // Первый раз событие существует
        mockEvent.Setup(x => x.HasEvent(eventId)).ReturnsAsync(true);
        mockEvent.Setup(x => x.GetById(eventId)).ReturnsAsync(eventDto);
        mockEvent.Setup(x => x.UpdateInternal(eventId, It.IsAny<EventDto>()))
            .ReturnsAsync((Guid id, EventDto dto) => dto);
    
        // Act - первая бронь создаётся
        var result1 = await service.CreateBookingAsync(eventId);
    
        // Assert - первая бронь создалась
        Assert.NotNull(result1);
    
        // Теперь событие "удалено" - HasEvent возвращает false И GetById возвращает null
        mockEvent.Setup(x => x.HasEvent(eventId)).ReturnsAsync(false);
        mockEvent.Setup(x => x.GetById(eventId)).ReturnsAsync((EventDto?)null);
    
        // Act & Assert - вторая бронь должна выбросить исключение
        await Assert.ThrowsAsync<NotFoundException>(() => 
            service.CreateBookingAsync(eventId));
    }
    
    /// <summary>
    /// Проверка получения брони по несуществующему Id
    /// </summary>
    [Fact]
    public async Task GetBookingByIdAsync_InvalidId_ReturnsNull()
    {
        // Arrange
        var (service, mockRepo, mockEvent) = CreateBookingService();
        var invalidId = Guid.NewGuid();
        mockRepo.Setup(x => x.GetByIdAsync(invalidId))
            .ReturnsAsync((BookingEntity?)null);
        
        // Act
        var result = await service.GetBookingByIdAsync(invalidId);
        
        // Assert
        Assert.Null(result);
    }

    #endregion
    
    #region Тесты смены статуса брони

    /// <summary>
    /// После вызова Confirm() бронь возвращает статус Confirmed и заполненный ProcessedAt
    /// </th>
    [Fact]
    public void Confirm_UpdatesStatusToConfirmedAndSetsProcessedAt()
    {
        // Arrange
        var booking = new BookingDto
        {
            Id = Guid.NewGuid(),
            EventId = Guid.NewGuid(),
            Status = BookingStatus.Pending,
            CreatedAt = DateTime.UtcNow.AddMinutes(-5),
            ProcessedAt = null
        };
        
        // Act
        booking.Confirm();
        
        // Assert
        Assert.Equal(BookingStatus.Confirmed, booking.Status);
        Assert.NotNull(booking.ProcessedAt);
        Assert.Equal(DateTimeKind.Utc, booking.ProcessedAt!.Value.Kind);
    }

    /// <summary>
    /// После вызова Reject() бронь возвращает статус Rejected и заполненный ProcessedAt
    /// </summary>
    [Fact]
    public void Reject_UpdatesStatusToRejectedAndSetsProcessedAt()
    {
        // Arrange
        var booking = new BookingDto
        {
            Id = Guid.NewGuid(),
            EventId = Guid.NewGuid(),
            Status = BookingStatus.Pending,
            CreatedAt = DateTime.UtcNow.AddMinutes(-5),
            ProcessedAt = null
        };
        
        // Act
        booking.Reject();
        
        // Assert
        Assert.Equal(BookingStatus.Rejected, booking.Status);
        Assert.NotNull(booking.ProcessedAt);
        Assert.Equal(DateTimeKind.Utc, booking.ProcessedAt!.Value.Kind);
    }

    /// <summary>
    /// После Reject() количество свободных мест восстанавливается
    /// </summary>
    [Fact]
    public void Reject_OnEventDto_ReleasesSeats()
    {
        // Arrange
        var eventItem = new EventDto
        {
            Id = Guid.NewGuid(),
            Title = "Test Event",
            TotalSeats = 10,
            AvailableSeats = 5
        };
        var initialAvailableSeats = eventItem.AvailableSeats;
        
        // Act
        eventItem.ReleaseSeats();
        
        // Assert
        Assert.Equal(initialAvailableSeats + 1, eventItem.AvailableSeats);
    }

    /// <summary>
    /// После Reject() можно успешно создать новую бронь на то же место
    /// </summary>
    [Fact]
    public async Task AfterReject_CanCreateNewBookingForSameSeat()
    {
        // Arrange
        var (service, mockRepo, mockEvent) = CreateBookingService();
        var eventId = Guid.NewGuid();
        var eventDto = new EventDto
        {
            Id = eventId,
            Title = "Test Event",
            TotalSeats = 1,
            AvailableSeats = 1
        };
        
        mockEvent.Setup(x => x.HasEvent(eventId)).ReturnsAsync(true);
        mockEvent.Setup(x => x.GetById(eventId))
            .ReturnsAsync(eventDto);
        
        mockEvent.Setup(x => x.UpdateInternal(eventId, It.IsAny<EventDto>()))
            .ReturnsAsync((Guid id, EventDto dto) => dto);
        
        mockRepo.Setup(x => x.AddAsync(It.IsAny<BookingEntity>()))
            .Returns(Task.CompletedTask)
            .Callback<BookingEntity>(booking => 
            {
                booking.Id = Guid.NewGuid();
            });
        
        // Act - создаём первую бронь
        var firstBooking = await service.CreateBookingAsync(eventId);
        Assert.NotNull(firstBooking);
        Assert.Equal(0, eventDto.AvailableSeats); // Место занято
        
        // Освобождаем место через Reject на DTO
        var bookingDto = new BookingDto
        {
            Id = firstBooking.Id,
            EventId = eventId,
            Status = BookingStatus.Pending
        };
        bookingDto.Reject();
        eventDto.ReleaseSeats(); // Восстанавливаем место
        
        // Act - создаём вторую бронь
        var secondBooking = await service.CreateBookingAsync(eventId);
        
        // Assert
        Assert.NotNull(secondBooking);
        Assert.NotEqual(firstBooking.Id, secondBooking.Id);
        Assert.Equal(0, eventDto.AvailableSeats); // Место снова занято
    }

    #endregion
}
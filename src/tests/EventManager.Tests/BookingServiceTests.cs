using AutoMapper;
using EventManager.Application.Interfaces;
using EventManager.Application.Services;
using EventManager.Enums;
using EventManager.Models;
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
        var (service, mockRepo, mockEvent) = CreateBookingService();
        var eventId = Guid.NewGuid();
        mockEvent.Setup(x => x.HasEvent(eventId)).ReturnsAsync(true);
        
        var result = await service.CreateBookingAsync(eventId);
        
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(eventId, result.EventId);
        Assert.Equal(BookingStatus.Pending, result.Status);
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
        mockEvent.Setup(x => x.HasEvent(eventId)).ReturnsAsync(true);
        
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
    /// Проверка создания брони для несуществующего события
    /// </summary>
    [Fact]
    public async Task CreateBookingAsync_NonExistentEvent_ReturnsNull()
    {
        // Arrange
        var (service, mockRepo, mockEvent) = CreateBookingService();
        var eventId = Guid.NewGuid();
        mockEvent.Setup(x => x.HasEvent(eventId)).ReturnsAsync(false);
        
        // Act
        var result = await service.CreateBookingAsync(eventId);
        
        // Assert
        Assert.Null(result);
        mockRepo.Verify(x => x.AddAsync(It.IsAny<BookingEntity>()), Times.Never);
    }
    
    /// <summary>
    /// Проверка создания брони для удалённого события
    /// </summary>
    [Fact]
    public async Task CreateBookingAsync_DeletedEvent_ReturnsNull()
    {
        // Arrange
        var (service, mockRepo, mockEvent) = CreateBookingService();
        var eventId = Guid.NewGuid();
        
        // Первый раз событие существует
        mockEvent.Setup(x => x.HasEvent(eventId)).ReturnsAsync(true);
        
        // Act - первая бронь создаётся
        var result1 = await service.CreateBookingAsync(eventId);
        
        // Assert - первая бронь создалась
        Assert.NotNull(result1);
        
        // Теперь событие "удалено"
        mockEvent.Setup(x => x.HasEvent(eventId)).ReturnsAsync(false);
        
        // Act - вторая бронь не создаётся
        var result2 = await service.CreateBookingAsync(eventId);
        
        // Assert
        Assert.Null(result2);
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
}
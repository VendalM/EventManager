using AutoMapper;
using EventManager.Application.Interfaces;
using EventManager.Application.Services;
using EventManager.Models;
using EventManager.Tests.TestData;
using Moq;

namespace EventManager.Tests;

/// <summary>
/// Набор тестов для проверки функциональности EventService
/// </summary>
public class EventServiceTests
{
    private readonly IMapper _mapper;
    
    public EventServiceTests()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<EventSaveDto, EventEntity>();
            cfg.CreateMap<EventEntity, EventDto>();
            cfg.CreateMap<EventSaveDto, EventDto>();
        });
        
        _mapper = config.CreateMapper();
    }
    
    private (EventService service, Mock<IEventRepository> mockRepo) CreateEventService()
    {
        var mockRepo = new Mock<IEventRepository>();
        var service = new EventService(_mapper, mockRepo.Object);
        return (service, mockRepo);
    }

    #region Успешные сценарии

    /// <summary>
    /// Проверка фильтрации событий по названию (регистронезависимый поиск)
    /// </summary>
    [Theory]
    [InlineData("Конференция", 2)]
    [InlineData("конференция", 2)]
    [InlineData("Встреча", 1)]
    [InlineData("встреча", 1)]
    [InlineData("Семинар", 1)]
    [InlineData("семинар", 1)]
    [InlineData("Хакатон", 1)]
    [InlineData("Несуществующее", 0)]
    [InlineData("", 5)]
    public async Task GetAllEvents_FilterByTitle_ReturnsFilteredEvents(string searchTitle, int expectedCount)
    {
        // Arrange
        var (service, mockRepo) = CreateEventService();
        var testEvents = EventTestData.GetTestEventsList()
            .Select(dto => _mapper.Map<EventEntity>(dto))
            .ToList();
        
        // Устанавливаем Id для тестовых событий
        for (int i = 0; i < testEvents.Count; i++)
        {
            testEvents[i].Id = Guid.NewGuid();
        }
        
        mockRepo.Setup(x => x.GetAllAsync()).ReturnsAsync(testEvents);
        
        // Act
        var result = await service.GetAllEvents(searchTitle, null, null, 1, 10);
        
        // Assert
        Assert.Equal(expectedCount, result.Items.Count);
    }

    /// <summary>
    /// Проверка получения всех событий без применения фильтров
    /// </summary>
    [Fact]
    public async Task GetAllEvents_NoFilters_ReturnsAllEvents()
    {
        // Arrange
        var (service, mockRepo) = CreateEventService();
        var testEvents = EventTestData.GetTestEventsList()
            .Select(dto => _mapper.Map<EventEntity>(dto))
            .ToList();
        
        for (int i = 0; i < testEvents.Count; i++)
        {
            testEvents[i].Id = Guid.NewGuid();
        }
        
        mockRepo.Setup(x => x.GetAllAsync()).ReturnsAsync(testEvents);
        
        // Act
        var result = await service.GetAllEvents(null, null, null, 1, 10);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.TotalItems);
        Assert.Equal(5, result.Items.Count);
    }

    /// <summary>
    /// Проверка получения события по существующему идентификатору
    /// </summary>
    [Fact]
    public async Task GetById_ValidId_ReturnsEvent()
    {
        // Arrange
        var (service, mockRepo) = CreateEventService();
        var eventId = Guid.NewGuid();
        var expectedEvent = new EventEntity
        {
            Id = eventId,
            Title = "Тестовое событие",
            Description = "Описание",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(1)
        };
        
        mockRepo.Setup(x => x.GetByIdAsync(eventId)).ReturnsAsync(expectedEvent);
        
        // Act
        var result = await service.GetById(eventId);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(eventId, result.Id);
        Assert.Equal("Тестовое событие", result.Title);
    }

    /// <summary>
    /// Проверка обновления существующего события
    /// </summary>
    [Fact]
    public async Task Update_ValidEvent_ReturnsUpdatedEvent()
    {
        // Arrange
        var (service, mockRepo) = CreateEventService();
        var eventId = Guid.NewGuid();
        var existingEvent = new EventEntity
        {
            Id = eventId,
            Title = "Старое название",
            Description = "Старое описание",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(1)
        };
        
        var updateData = new EventSaveDto
        {
            Title = "Обновленное название",
            Description = "Обновленное описание",
            StartDate = DateTime.UtcNow.AddDays(2),
            EndDate = DateTime.UtcNow.AddDays(3)
        };
        
        mockRepo.Setup(x => x.GetByIdAsync(eventId)).ReturnsAsync(existingEvent);
        mockRepo.Setup(x => x.UpdateAsync(It.IsAny<EventEntity>())).Returns(Task.CompletedTask);
        
        // Act
        var result = await service.Update(eventId, updateData);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(eventId, result.Id);
        Assert.Equal("Обновленное название", result.Title);
        mockRepo.Verify(x => x.UpdateAsync(It.IsAny<EventEntity>()), Times.Once);
    }

    /// <summary>
    /// Проверка удаления существующего события
    /// </summary>
    [Fact]
    public async Task Delete_ValidId_ReturnsTrue()
    {
        // Arrange
        var (service, mockRepo) = CreateEventService();
        var eventId = Guid.NewGuid();
        
        mockRepo.Setup(x => x.RemoveAsync(eventId)).ReturnsAsync(true);
        
        // Act
        var result = await service.Delete(eventId);
        
        // Assert
        Assert.True(result);
        mockRepo.Verify(x => x.RemoveAsync(eventId), Times.Once);
    }

    /// <summary>
    /// Проверка фильтрации событий по диапазону дат
    /// </summary>
    [Theory]
    [InlineData(2, 4, 2)]
    [InlineData(1, 1, 1)]
    [InlineData(6, 10, 0)]
    public async Task GetAllEvents_FilterByDateRange_ReturnsFilteredEvents(int fromDays, int toDays, int expectedCount)
    {
        // Arrange
        var (service, mockRepo) = CreateEventService();
        var fixedDate = new DateTime(2024, 12, 20);
        
        var testEvents = new List<EventEntity>
        {
            new() { Id = Guid.NewGuid(), Title = "Событие 1", StartDate = fixedDate.AddDays(2), EndDate = fixedDate.AddDays(3) },
            new() { Id = Guid.NewGuid(), Title = "Событие 2", StartDate = fixedDate.AddDays(3), EndDate = fixedDate.AddDays(4) },
            new() { Id = Guid.NewGuid(), Title = "Событие 3", StartDate = fixedDate.AddDays(1), EndDate = fixedDate.AddDays(1) },
            new() { Id = Guid.NewGuid(), Title = "Событие 4", StartDate = fixedDate.AddDays(4), EndDate = fixedDate.AddDays(5) },
            new() { Id = Guid.NewGuid(), Title = "Событие 5", StartDate = fixedDate.AddDays(5), EndDate = fixedDate.AddDays(6) }
        };
        
        mockRepo.Setup(x => x.GetAllAsync()).ReturnsAsync(testEvents);
        
        var from = fixedDate.AddDays(fromDays);
        var to = fixedDate.AddDays(toDays);
        
        // Act
        var result = await service.GetAllEvents(null, from, to, 1, 10);
        
        // Assert
        Assert.Equal(expectedCount, result.Items.Count);
    }

    /// <summary>
    /// Проверка пагинации событий
    /// </summary>
    [Theory]
    [InlineData(1, 10, 10, 25)]
    [InlineData(2, 10, 10, 25)]
    [InlineData(3, 10, 5, 25)]
    public async Task GetAllEvents_Pagination_ReturnsCorrectPage(int page, int pageSize, int expectedCount, int expectedTotal)
    {
        // Arrange
        var (service, mockRepo) = CreateEventService();
        var testEvents = new List<EventEntity>();
        
        for (int i = 1; i <= 25; i++)
        {
            testEvents.Add(new EventEntity
            {
                Id = Guid.NewGuid(),
                Title = $"Событие {i}",
                StartDate = DateTime.UtcNow.AddDays(i),
                EndDate = DateTime.UtcNow.AddDays(i + 1)
            });
        }
        
        mockRepo.Setup(x => x.GetAllAsync()).ReturnsAsync(testEvents);
        
        // Act
        var result = await service.GetAllEvents(null, null, null, page, pageSize);
        
        // Assert
        Assert.Equal(expectedCount, result.Items.Count);
        Assert.Equal(expectedTotal, result.TotalItems);
    }

    /// <summary>
    /// Проверка комбинированной фильтрации (по названию и диапазону дат)
    /// </summary>
    [Fact]
    public async Task GetAllEvents_CombinedFilters_ReturnsCorrectEvents()
    {
        // Arrange
        var (service, mockRepo) = CreateEventService();
        var fixedDate = new DateTime(2024, 12, 20);
        
        var testEvents = new List<EventEntity>
        {
            new() { Id = Guid.NewGuid(), Title = "Конференция по IT", StartDate = fixedDate.AddDays(2), EndDate = fixedDate.AddDays(3) },
            new() { Id = Guid.NewGuid(), Title = "Конференция по дизайну", StartDate = fixedDate.AddDays(3), EndDate = fixedDate.AddDays(4) },
            new() { Id = Guid.NewGuid(), Title = "Встреча команды", StartDate = fixedDate.AddDays(1), EndDate = fixedDate.AddDays(1) },
            new() { Id = Guid.NewGuid(), Title = "Семинар по тестированию", StartDate = fixedDate.AddDays(4), EndDate = fixedDate.AddDays(5) },
            new() { Id = Guid.NewGuid(), Title = "Хакатон", StartDate = fixedDate.AddDays(5), EndDate = fixedDate.AddDays(6) }
        };
        
        mockRepo.Setup(x => x.GetAllAsync()).ReturnsAsync(testEvents);
        
        var from = fixedDate.AddDays(2);
        var to = fixedDate.AddDays(4);
        
        // Act
        var result = await service.GetAllEvents("Конференция", from, to, 1, 10);
        
        // Assert
        Assert.Equal(2, result.Items.Count);
        Assert.All(result.Items, e => Assert.Contains("Конференция", e.Title));
    }

    #endregion

    #region Неуспешные сценарии

    /// <summary>
    /// Проверка получения события с несуществующим идентификатором
    /// </summary>
    [Fact]
    public async Task GetById_InvalidId_ReturnsNull()
    {
        // Arrange
        var (service, mockRepo) = CreateEventService();
        var invalidId = Guid.NewGuid();
        
        mockRepo.Setup(x => x.GetByIdAsync(invalidId)).ReturnsAsync((EventEntity?)null);
        
        // Act
        var result = await service.GetById(invalidId);
        
        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Проверка обновления события с несуществующим идентификатором
    /// </summary>
    [Fact]
    public async Task Update_InvalidId_ReturnsNull()
    {
        // Arrange
        var (service, mockRepo) = CreateEventService();
        var invalidId = Guid.NewGuid();
        
        mockRepo.Setup(x => x.GetByIdAsync(invalidId)).ReturnsAsync((EventEntity?)null);
        
        var updateData = new EventSaveDto
        {
            Title = "Обновление",
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(2)
        };
        
        // Act
        var result = await service.Update(invalidId, updateData);
        
        // Assert
        Assert.Null(result);
        mockRepo.Verify(x => x.UpdateAsync(It.IsAny<EventEntity>()), Times.Never);
    }

    /// <summary>
    /// Проверка удаления события с несуществующим идентификатором
    /// </summary>
    [Fact]
    public async Task Delete_InvalidId_ReturnsFalse()
    {
        // Arrange
        var (service, mockRepo) = CreateEventService();
        var invalidId = Guid.NewGuid();
        
        mockRepo.Setup(x => x.RemoveAsync(invalidId)).ReturnsAsync(false);
        
        // Act
        var result = await service.Delete(invalidId);
        
        // Assert
        Assert.False(result);
    }

    #endregion
}
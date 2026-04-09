using AutoMapper;
using EventManager.Application.Interfaces;
using EventManager.Application.Repositories;
using EventManager.Application.Services;
using EventManager.Models;
using EventManager.Tests.TestData;

namespace EventManager.Tests;

/// <summary>
/// Набор тестов для проверки функциональности EventService
/// </summary>
public class EventServiceTests
{
    private readonly EventService _eventService;
    private readonly IEventRepository _eventRepository;
    
    /// <summary>
    /// Конструктор, который настраивает AutoMapper и создает экземпляр EventService для тестов
    /// </summary>
    public EventServiceTests()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<EventSaveDto, EventEntity>();
            cfg.CreateMap<EventEntity, EventDto>();
            cfg.CreateMap<EventSaveDto, EventDto>();
        });
        
        var mapper = config.CreateMapper();
        _eventRepository = new EventRepository();
        _eventService = new EventService(mapper, _eventRepository);
        
        ClearAllEvents();
    }
    
    /// <summary>
    /// Очистка всех событий через репозиторий
    /// </summary>
    private void ClearAllEvents()
    {
        var events = _eventRepository.GetAllAsync().Result;
        foreach (var eventEntity in events)
        {
            _eventRepository.RemoveAsync(eventEntity.Id).Wait();
        }
    }
    
    /// <summary>
    /// Создание тестовых событий для проверки фильтрации
    /// </summary>
    private void CreateTestEvents()
    {
        var testEvents = EventTestData.GetTestEventsList();
        foreach (var testEvent in testEvents)
        {
            _eventService.Create(testEvent).Wait();
        }
    }
    
    /// <summary>
    /// Получить первый ID из репозитория
    /// </summary>
    private Guid GetFirstEventId()
    {
        return _eventRepository.GetAllAsync().Result.First().Id;
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
    public void GetAllEvents_FilterByTitle_ReturnsFilteredEvents(string searchTitle, int expectedCount)
    {
        ClearAllEvents();
        CreateTestEvents();
        
        var result = _eventService.GetAllEvents(searchTitle, null, null, 1, 10).Result;
        
        Assert.Equal(expectedCount, result.Items.Count);
    }

    /// <summary>
    /// Проверка получения всех событий без применения фильтров
    /// </summary>
    [Fact]
    public void GetAllEvents_NoFilters_ReturnsAllEvents()
    {
        ClearAllEvents();
        CreateTestEvents();
        
        var result = _eventService.GetAllEvents(null, null, null, 1, 10).Result;
        
        Assert.NotNull(result);
        Assert.Equal(5, result.TotalItems);
        Assert.Equal(5, result.Items.Count);
    }

    /// <summary>
    /// Проверка получения события по существующему идентификатору
    /// </summary>
    [Fact]
    public void GetById_ValidId_ReturnsEvent()
    {
        ClearAllEvents();
        CreateTestEvents();
    
        var firstId = GetFirstEventId();
    
        var result = _eventService.GetById(firstId).Result;
    
        Assert.NotNull(result);
        Assert.Equal(firstId, result.Id);
    }

    /// <summary>
    /// Проверка обновления существующего события
    /// </summary>
    [Fact]
    public void Update_ValidEvent_ReturnsUpdatedEvent()
    {
        ClearAllEvents();
        CreateTestEvents();
        
        var firstId = GetFirstEventId();
        
        var updateData = new EventSaveDto
        {
            Title = "Обновленное название",
            Description = "Обновленное описание",
            StartDate = EventTestData.FixedDate.AddDays(10),
            EndDate = EventTestData.FixedDate.AddDays(11)
        };
        
        var result = _eventService.Update(firstId, updateData).Result;
        
        Assert.NotNull(result);
        Assert.Equal(firstId, result.Id);
        Assert.Equal("Обновленное название", result.Title);
    }

    /// <summary>
    /// Проверка удаления существующего события
    /// </summary>
    [Fact]
    public void Delete_ValidId_ReturnsTrue()
    {
        ClearAllEvents();
        CreateTestEvents();
        
        var firstId = GetFirstEventId();
        
        var result = _eventService.Delete(firstId).Result;
        
        Assert.True(result);
        Assert.Null(_eventService.GetById(firstId).Result);
    }

    /// <summary>
    /// Проверка фильтрации событий по диапазону дат
    /// </summary>
    [Theory]
    [InlineData(2, 4, 2)]
    [InlineData(1, 1, 1)]
    [InlineData(6, 10, 0)]
    public void GetAllEvents_FilterByDateRange_ReturnsFilteredEvents(int fromDays, int toDays, int expectedCount)
    {
        ClearAllEvents();
        CreateTestEvents();
        
        var from = EventTestData.FixedDate.AddDays(fromDays);
        var to = EventTestData.FixedDate.AddDays(toDays);
        
        var result = _eventService.GetAllEvents(null, from, to, 1, 10).Result;
        
        Assert.Equal(expectedCount, result.Items.Count);
    }

    /// <summary>
    /// Проверка пагинации событий
    /// </summary>
    [Theory]
    [InlineData(1, 10, 10, 25)]
    [InlineData(2, 10, 10, 25)]
    [InlineData(3, 10, 5, 25)]
    [InlineData(1, 5, 5, 25)]
    [InlineData(5, 5, 5, 25)]
    [InlineData(1, 20, 20, 25)]
    public void GetAllEvents_Pagination_ReturnsCorrectPage(int page, int pageSize, int expectedCount, int expectedTotal)
    {
        ClearAllEvents();
        
        for (int i = 1; i <= 25; i++)
        {
            _eventService.Create(new EventSaveDto 
            { 
                Title = $"Событие {i}", 
                StartDate = EventTestData.FixedDate.AddDays(i), 
                EndDate = EventTestData.FixedDate.AddDays(i + 1) 
            }).Wait();
        }
        
        var result = _eventService.GetAllEvents(null, null, null, page, pageSize).Result;
        
        Assert.Equal(expectedCount, result.Items.Count);
        Assert.Equal(expectedTotal, result.TotalItems);
    }

    /// <summary>
    /// Проверка комбинированной фильтрации (по названию и диапазону дат)
    /// </summary>
    [Fact]
    public void GetAllEvents_CombinedFilters_ReturnsCorrectEvents()
    {
        ClearAllEvents();
        CreateTestEvents();
        
        var from = EventTestData.FixedDate.AddDays(2);
        var to = EventTestData.FixedDate.AddDays(4);
        
        var result = _eventService.GetAllEvents("Конференция", from, to, 1, 10).Result;
        
        Assert.Equal(2, result.Items.Count);
        Assert.All(result.Items, e => Assert.Contains("Конференция", e.Title));
    }

    #endregion

    #region Неуспешные сценарии

    /// <summary>
    /// Проверка получения события с несуществующим идентификатором
    /// </summary>
    [Fact]
    public void GetById_InvalidId_ReturnsNull()
    {
        ClearAllEvents();
        
        var result = _eventService.GetById(Guid.NewGuid()).Result;
        
        Assert.Null(result);
    }

    /// <summary>
    /// Проверка обновления события с несуществующим идентификатором
    /// </summary>
    [Fact]
    public void Update_InvalidId_ReturnsNull()
    {
        ClearAllEvents();
        
        var updateData = new EventSaveDto
        {
            Title = "Обновление",
            StartDate = EventTestData.FixedDate.AddDays(1),
            EndDate = EventTestData.FixedDate.AddDays(2)
        };
        
        var result = _eventService.Update(Guid.NewGuid(), updateData).Result;
        
        Assert.Null(result);
    }

    /// <summary>
    /// Проверка удаления события с несуществующим идентификатором
    /// </summary>
    [Fact]
    public void Delete_InvalidId_ReturnsFalse()
    {
        ClearAllEvents();
        
        var result = _eventService.Delete(Guid.NewGuid()).Result;
        
        Assert.False(result);
    }

    #endregion
}
using EventManager.Infrastructure.DataAccess;
using EventManager.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;

namespace EventManager.Tests;

/// <summary>
/// Тесты для проверки структуры базы данных и конфигурации контекста (InMemory).
/// </summary>
public class AppDbContextStructureTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;

    public AppDbContextStructureTests()
    {
        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(dbName));
        _serviceProvider = services.BuildServiceProvider();
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
    }

    /// <summary>
    /// Проверяет, что контекст базы данных создаётся без ошибок.
    /// </summary>
    [Fact]
    public void AppDbContext_CanBeCreated()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.NotNull(context);
    }

    /// <summary>
    /// Проверяет, что DbSet Events не равен null.
    /// </summary>
    [Fact]
    public void AppDbContext_EventsDbSet_IsNotNull()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.NotNull(context.Events);
    }

    /// <summary>
    /// Проверяет, что DbSet Bookings не равен null.
    /// </summary>
    [Fact]
    public void AppDbContext_BookingsDbSet_IsNotNull()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.NotNull(context.Bookings);
    }

    /// <summary>
    /// Проверяет, что EnsureCreated не выбрасывает исключений (создаёт схему).
    /// </summary>
    [Fact]
    public void AppDbContext_EnsureCreated_Works()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var created = context.Database.EnsureCreated();
        Assert.True(created);
    }

    /// <summary>
    /// Проверяет, что модель содержит сущность EventEntity.
    /// </summary>
    [Fact]
    public void Model_ContainsEventEntity()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var entityType = context.Model.FindEntityType(typeof(EventEntity));
        Assert.NotNull(entityType);
    }

    /// <summary>
    /// Проверяет, что модель содержит сущность BookingEntity.
    /// </summary>
    [Fact]
    public void Model_ContainsBookingEntity()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var entityType = context.Model.FindEntityType(typeof(BookingEntity));
        Assert.NotNull(entityType);
    }

    /// <summary>
    /// Проверяет, что таблица событий имеет правильное имя "events".
    /// </summary>
    [Fact]
    public void EventEntity_TableName_IsEvents()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var entityType = context.Model.FindEntityType(typeof(EventEntity));
        Assert.NotNull(entityType);
        var tableName = entityType!.GetTableName();
        Assert.Equal("events", tableName);
    }

    /// <summary>
    /// Проверяет, что таблица бронирований имеет правильное имя "bookings".
    /// </summary>
    [Fact]
    public void BookingEntity_TableName_IsBookings()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var entityType = context.Model.FindEntityType(typeof(BookingEntity));
        Assert.NotNull(entityType);
        var tableName = entityType!.GetTableName();
        Assert.Equal("bookings", tableName);
    }

    /// <summary>
    /// Проверяет, что у EventEntity есть навигационное свойство Bookings.
    /// </summary>
    [Fact]
    public void EventEntity_HasNavigationToBookings()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var entityType = context.Model.FindEntityType(typeof(EventEntity));
        Assert.NotNull(entityType);
        var navigation = entityType!.GetNavigations().FirstOrDefault(n => n.Name == "Bookings");
        Assert.NotNull(navigation);
        Assert.True(navigation!.IsCollection);
    }

    /// <summary>
    /// Проверяет, что у BookingEntity есть навигационное свойство Event.
    /// </summary>
    [Fact]
    public void BookingEntity_HasNavigationToEvent()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var entityType = context.Model.FindEntityType(typeof(BookingEntity));
        Assert.NotNull(entityType);
        var navigation = entityType!.GetNavigations().FirstOrDefault(n => n.Name == "Event");
        Assert.NotNull(navigation);
        Assert.False(navigation!.IsCollection);
    }

    /// <summary>
    /// Проверяет, что свойство Id у EventEntity является первичным ключом и генерируется в коде (ValueGeneratedNever).
    /// </summary>
    [Fact]
    public void EventEntity_Id_IsPrimaryKey_AndValueGeneratedNever()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var entityType = context.Model.FindEntityType(typeof(EventEntity));
        Assert.NotNull(entityType);
        var pk = entityType!.FindPrimaryKey();
        Assert.NotNull(pk);
        var idProperty = pk!.Properties.FirstOrDefault(p => p.Name == "Id");
        Assert.NotNull(idProperty);
        Assert.Equal(ValueGenerated.Never, idProperty!.ValueGenerated);
    }

    /// <summary>
    /// Проверяет, что свойство Id у BookingEntity является первичным ключом и генерируется в коде (ValueGeneratedNever).
    /// </summary>
    [Fact]
    public void BookingEntity_Id_IsPrimaryKey_AndValueGeneratedNever()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var entityType = context.Model.FindEntityType(typeof(BookingEntity));
        Assert.NotNull(entityType);
        var pk = entityType!.FindPrimaryKey();
        Assert.NotNull(pk);
        var idProperty = pk!.Properties.FirstOrDefault(p => p.Name == "Id");
        Assert.NotNull(idProperty);
        Assert.Equal(ValueGenerated.Never, idProperty!.ValueGenerated);
    }

    /// <summary>
    /// Проверяет, что Title в EventEntity имеет максимальную длину 200 и обязателен для заполнения.
    /// </summary>
    [Fact]
    public void EventEntity_Title_HasMaxLength200_AndIsRequired()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var entityType = context.Model.FindEntityType(typeof(EventEntity));
        Assert.NotNull(entityType);
        var property = entityType!.FindProperty("Title");
        Assert.NotNull(property);
        var maxLength = property!.GetMaxLength();
        Assert.Equal(200, maxLength);
        Assert.False(property.IsNullable);
    }

    /// <summary>
    /// Проверяет, что Description в EventEntity имеет максимальную длину 2000.
    /// </summary>
    [Fact]
    public void EventEntity_Description_HasMaxLength2000()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var entityType = context.Model.FindEntityType(typeof(EventEntity));
        Assert.NotNull(entityType);
        var property = entityType!.FindProperty("Description");
        Assert.NotNull(property);
        var maxLength = property!.GetMaxLength();
        Assert.Equal(2000, maxLength);
    }

    /// <summary>
    /// Проверяет, что StartDate и EndDate в EventEntity обязательны для заполнения.
    /// </summary>
    [Fact]
    public void EventEntity_StartDateAndEndDate_AreRequired()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var entityType = context.Model.FindEntityType(typeof(EventEntity));
        Assert.NotNull(entityType);
        var startDateProp = entityType!.FindProperty("StartDate");
        var endDateProp = entityType.FindProperty("EndDate");
        Assert.NotNull(startDateProp);
        Assert.NotNull(endDateProp);
        Assert.False(startDateProp!.IsNullable);
        Assert.False(endDateProp!.IsNullable);
    }

    /// <summary>
    /// Проверяет, что TotalSeats и AvailableSeats в EventEntity обязательны для заполнения.
    /// </summary>
    [Fact]
    public void EventEntity_Seats_AreRequired()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var entityType = context.Model.FindEntityType(typeof(EventEntity));
        Assert.NotNull(entityType);
        var totalSeatsProp = entityType!.FindProperty("TotalSeats");
        var availableSeatsProp = entityType.FindProperty("AvailableSeats");
        Assert.NotNull(totalSeatsProp);
        Assert.NotNull(availableSeatsProp);
        Assert.False(totalSeatsProp!.IsNullable);
        Assert.False(availableSeatsProp!.IsNullable);
    }

    /// <summary>
    /// Проверяет, что у BookingEntity внешний ключ EventId не равен null.
    /// </summary>
    [Fact]
    public void BookingEntity_EventId_IsRequired()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var entityType = context.Model.FindEntityType(typeof(BookingEntity));
        Assert.NotNull(entityType);
        var property = entityType!.FindProperty("EventId");
        Assert.NotNull(property);
        Assert.False(property!.IsNullable);
    }

    /// <summary>
    /// Проверяет, что Status в BookingEntity имеет максимальную длину 20 (соответствует эталону).
    /// </summary>
    [Fact]
    public void BookingEntity_Status_HasMaxLength20()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var entityType = context.Model.FindEntityType(typeof(BookingEntity));
        Assert.NotNull(entityType);
        var property = entityType!.FindProperty("Status");
        Assert.NotNull(property);
        var maxLength = property!.GetMaxLength();
        Assert.Equal(20, maxLength);
    }
}
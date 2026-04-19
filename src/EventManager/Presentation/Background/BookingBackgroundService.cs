using EventManager.Application.Interfaces;
using EventManager.Enums;
using EventManager.Models;

namespace EventManager.Presentation.Background;

/// <summary>
/// Сервис для выполнения фоновых задач, связанных с бронированием событий.
/// </summary>
public class BookingBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BookingBackgroundService> _logger;
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(5); // Опрос каждые 5 секунд
    
    /// <summary>
    /// Конструктор для BookingBackgroundService, который принимает фабрику для создания области видимости.
    /// </summary>
    /// <param name="scopeFactory">Фабрика для создания области видимости, которая позволяет получать сервисы из DI-контейнера.</param>
    /// <param name="logger"> Логгер для записи информации о выполнении фоновых задач.</param>
    public BookingBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<BookingBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }
    
    /// <summary>
    /// Метод, который выполняется в фоновом режиме для обработки задач бронирования.
    /// </summary>
    /// <param name="stoppingToken">Токен отмены, который сигнализирует о необходимости остановки сервиса.</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Сервис бронирования запущен в фоновом режиме.");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessBookings(stoppingToken);
            await Task.Delay(_pollingInterval, stoppingToken);
        }
        
        _logger.LogInformation("Сервис бронирования остановлен.");
    }
    
    /// <summary>
    /// Метод для обработки бронирований со статусом Pending. 
    /// </summary>
    /// <param name="stoppingToken">Токен отмены, который сигнализирует о необходимости остановки сервиса.</param>
    private async Task ProcessBookings(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IBookingRepository>();
        
        var pending = (await repo.GetAllAsync())
            .Where(b => b.Status == BookingStatus.Pending)
            .ToList();
        
        if (pending.Count == 0) return;
        
        _logger.LogInformation("Найдено {Count} бронирований со статусом Pending для обработки", pending.Count);
        
        foreach (var booking in pending)
        {
            if (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            
            await ProcessBooking(repo, booking, stoppingToken);
        }
    }
    
    /// <summary>
    /// Метод для обработки одного бронирования. В данном примере он просто имитирует обработку, устанавливая статус в Confirmed после задержки.
    /// </summary>
    /// <param name="repo">Репозиторий для доступа к данным бронирования.</param>
    /// <param name="booking">Сущность бронирования, которую нужно обработать.</param>
    /// <param name="ct">Токен отмены, который сигнализирует о необходимости остановки сервиса.</param>
    private async Task ProcessBooking(IBookingRepository repo, BookingEntity booking, CancellationToken ct)
    {
        try
        {
            await Task.Delay(2000, ct);
            booking.Status = BookingStatus.Confirmed;
            booking.ProcessedAt = DateTime.Now;
            await repo.UpdateAsync(booking);
            _logger.LogInformation("Бронирование {Id} успешно обработано и подтверждено", booking.Id);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Ошибка при обработке бронирования с Id={BookingId}", booking.Id);
        }
    }
}
using AutoMapper;
using EventManager.Application;
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
    private readonly IMapper _mapper;
 
    /// <summary>
    /// Конструктор для BookingBackgroundService, который принимает фабрику для создания области видимости.
    /// </summary>
    /// <param name="scopeFactory">Фабрика для создания области видимости, которая позволяет получать сервисы из DI-контейнера.</param>
    /// <param name="logger"> Логгер для записи информации о выполнении фоновых задач.</param>
    /// <param name="mapper"> AutoMapper для преобразования между сущностями и DTO, который будет использоваться при обработке бронирований.</param>
    public BookingBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<BookingBackgroundService> logger,
        IMapper mapper)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _mapper = mapper;
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
        var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();
        
        var pending = (await bookingRepository.GetAllAsync())
            .Where(b => b.Status == BookingStatus.Pending)
            .ToList();
        
        if (pending.Count == 0) return;
        
        _logger.LogInformation("Найдено {Count} бронирований со статусом Pending для обработки", pending.Count);
        
        var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();

        var tasks = pending.Select(async booking => await ProcessBooking(eventRepository, bookingRepository, _mapper.Map<BookingDto>(booking), stoppingToken));
        await Task.WhenAll(tasks); 
    }
    
    /// <summary>
    /// Метод для обработки одного бронирования. В данном примере он просто имитирует обработку, устанавливая статус в Confirmed после задержки.
    /// </summary>
    /// /// <param name="eventRepository"> Репозиторий для доступа к данным событий, который будет использоваться для проверки существования событий при обработке бронирований.</param>
    /// <param name="bookingRepository"> Репозиторий для доступа к данным бронирований, который будет использоваться для получения и обновления статуса бронирований.</param>
    /// <param name="booking">Сущность бронирования, которую нужно обработать.</param>
    /// <param name="ct">Токен отмены, который сигнализирует о необходимости остановки сервиса.</param>
    private async Task ProcessBooking(IEventRepository eventRepository, IBookingRepository bookingRepository, BookingDto booking, CancellationToken ct)
    {
        await Task.Delay(2000, ct);
        var hasEvent = await eventRepository.HasEventAsync(booking.EventId);
        
        if (!hasEvent)
        {
            booking.Reject();
            await bookingRepository.UpdateAsync(_mapper.Map<BookingEntity>(booking));
            _logger.LogWarning("В бронировании {Id} отказано в связи с отсутствием события для бронирования",
                booking.Id);
            return;
        }
        
        await EventSemaphore.Semaphore.WaitAsync(ct);
        try
        {
            booking.Confirm();
            await bookingRepository.UpdateAsync(_mapper.Map<BookingEntity>(booking));
            _logger.LogInformation("Бронирование {Id} успешно обработано и подтверждено", booking.Id);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Ошибка при обработке бронирования с Id={BookingId}", booking.Id);

            try
            {
                booking.Reject();
                await bookingRepository.UpdateAsync(_mapper.Map<BookingEntity>(booking));
                
                var eventForBooking = await eventRepository.GetByIdAsync(booking.EventId);
                if (eventForBooking != null)
                {
                    var eventForBookingDto = _mapper.Map<EventDto>(eventForBooking);
                    eventForBookingDto.ReleaseSeats();
                    await eventRepository.UpdateAsync(_mapper.Map<EventEntity>(eventForBookingDto));
                }
            }
            catch (Exception innerEx)
            {
                _logger.LogError(innerEx, "Не удалось отклонить бронь {BookingId} после ошибки", booking.Id);
            }
        }
        finally
        {
            EventSemaphore.Semaphore.Release();
        }
    }
}
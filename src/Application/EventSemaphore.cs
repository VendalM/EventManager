namespace Application;

/// <summary>
/// Глобальный семафор для синхронизации операций над событиями и бронированиями
/// </summary>
public static class EventSemaphore
{
    /// <summary>
    /// Единый семафор, блокирующий одновременное изменение мест и обновление TotalSeats
    /// </summary>
    public static readonly SemaphoreSlim Semaphore = new(1, 1);
}
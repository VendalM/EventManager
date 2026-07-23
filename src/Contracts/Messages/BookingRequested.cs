namespace Contracts.Messages;

/// <summary>
/// Бронь направлена на подтверждение
/// </summary>
public sealed record BookingRequested(
    Guid BookingId,
    Guid EventId,
    Guid UserId,
    int SeatsCount,
    DateTime RequestedAtUtc
);
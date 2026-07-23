using Contracts.Enums;

namespace Contracts.Messages;

/// <summary>
/// Бронь отклонена
/// </summary>
public sealed record BookingRejected(
    Guid BookingId,
    Guid EventId,
    Guid UserId,
    int SeatsCount,
    BookingRejectionReason Reason,
    DateTime RejectedAtUtc
);
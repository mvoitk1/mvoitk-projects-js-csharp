namespace VenuePlatform.Contracts.Bookings;

public sealed record BookingResponse(
    Guid Id,
    Guid ClientId,
    string Title,
    DateTime StartUtc,
    DateTime EndUtc,
    int AttendeeCount,
    bool IsCancelled,
    IReadOnlyList<Guid> SpaceIds,
    decimal TotalAmount,
    Guid? SpaceConfigurationId,
    BookingStatus Status,
    DateTime? CancelledUtc,
    string? CancelReason
);

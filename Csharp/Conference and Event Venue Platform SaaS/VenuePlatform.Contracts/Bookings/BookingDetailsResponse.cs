namespace VenuePlatform.Contracts.Bookings;

public sealed record BookingDetailsResponse(
    Guid Id,
    Guid ClientId,
    string ClientName,
    string Title,
    DateTime StartUtc,
    DateTime EndUtc,
    int AttendeeCount,
    bool IsCancelled,
    decimal TotalAmount,
    Guid? SpaceConfigurationId,
    IReadOnlyList<SpaceSummary> Spaces,
    BookingStatus Status,
    DateTime? CancelledUtc,
    string? CancelReason
);

public sealed record SpaceSummary(Guid Id, string Name);

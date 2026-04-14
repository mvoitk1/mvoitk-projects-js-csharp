namespace VenuePlatform.Contracts.Bookings;

public sealed record CreateBookingRequest(
    Guid ClientId,
    string Title,
    DateTime StartUtc,
    DateTime EndUtc,
    int AttendeeCount,
    Guid? SpaceConfigurationId = null
);

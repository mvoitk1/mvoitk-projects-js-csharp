namespace VenuePlatform.Contracts.Bookings;

public sealed record UpdateBookingRequest(
    string Title,
    DateTime StartUtc,
    DateTime EndUtc,
    int AttendeeCount,
    Guid? SpaceConfigurationId = null
);

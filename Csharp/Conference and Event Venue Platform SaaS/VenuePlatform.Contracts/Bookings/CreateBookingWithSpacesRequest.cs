namespace VenuePlatform.Contracts.Bookings;

public sealed record CreateBookingWithSpacesRequest(
    Guid ClientId,
    string Title,
    DateTime StartUtc,
    DateTime EndUtc,
    int AttendeeCount,
    IReadOnlyList<Guid> SpaceIds,
    Guid? SpaceConfigurationId = null
);

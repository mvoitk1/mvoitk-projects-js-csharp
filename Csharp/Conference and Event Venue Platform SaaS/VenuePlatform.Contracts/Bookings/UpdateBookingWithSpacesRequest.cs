namespace VenuePlatform.Contracts.Bookings;

public sealed record UpdateBookingWithSpacesRequest(
    string Title,
    DateTime StartUtc,
    DateTime EndUtc,
    int AttendeeCount,
    IReadOnlyList<Guid> SpaceIds,
    Guid? SpaceConfigurationId = null
);

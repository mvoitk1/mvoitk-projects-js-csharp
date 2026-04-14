namespace VenuePlatform.Contracts.BookingRequests;

public sealed record CreateBookingRequestRequest(
    string ContactName,
    string ContactEmail,
    string? ContactPhone,
    string? Notes,
    DateTime StartUtc,
    DateTime EndUtc,
    List<Guid> SpaceIds
);

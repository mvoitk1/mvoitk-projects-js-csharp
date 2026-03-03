namespace VenuePlatform.Contracts.Bookings;

public sealed record SetBookingSpacesRequest(IReadOnlyList<Guid> SpaceIds);

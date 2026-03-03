namespace VenuePlatform.Contracts.Bookings;

public sealed record BookingConflictResponse(
    string Error,
    IReadOnlyList<Guid> ConflictingBookingIds,
    IReadOnlyList<Guid> ConflictingSpaceIds
);

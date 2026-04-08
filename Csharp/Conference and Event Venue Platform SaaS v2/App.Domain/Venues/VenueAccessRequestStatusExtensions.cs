namespace App.Domain.Venues;

public static class VenueAccessRequestStatusExtensions
{
    public static bool AllowsMembershipAssignment(this VenueAccessRequestStatus status) =>
        status == VenueAccessRequestStatus.Approved;

    public static bool IsTerminal(this VenueAccessRequestStatus status) =>
        status is VenueAccessRequestStatus.Approved or VenueAccessRequestStatus.Rejected;
}

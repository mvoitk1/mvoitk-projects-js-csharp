namespace App.DTO.v1.Venues.Admin;

public class VenueAdminDashboardDto
{
    public string VenueName { get; init; } = default!;
    public int ActiveSpacesCount { get; init; }
    public int UpcomingBookingsCount { get; init; }
    public int PendingApprovalBookingsCount { get; init; }
    public int PendingRequestCount { get; init; }
    public int ActiveMembershipCount { get; init; }
    public IReadOnlyList<string> ActionItems { get; init; } = [];
}

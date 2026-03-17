namespace App.DTO.v1.Venues.Admin;

public class VenueAccessRequestDetailDto : VenueAccessRequestListItemDto
{
    public Guid RequestorUserId { get; init; }
    public string? RequestorEmail { get; init; }
    public Guid? ReviewedByUserId { get; init; }
    public string? ReviewedByEmail { get; init; }
    public Guid? CompanyId { get; init; }
    public Guid? VenueId { get; init; }
    public string? ContactPhone { get; init; }
    public string AddressLine1 { get; init; } = default!;
    public string? Notes { get; init; }
    public string? ReviewNotes { get; init; }
    public string? ApprovedAccessLevel { get; init; }
    public VenueMembershipAssignmentDto? AssignedMembership { get; init; }
}

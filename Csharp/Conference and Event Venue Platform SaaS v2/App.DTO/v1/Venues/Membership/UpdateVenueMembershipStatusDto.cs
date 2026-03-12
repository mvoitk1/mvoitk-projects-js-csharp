namespace App.DTO.v1.Venues.Membership;

public class UpdateVenueMembershipStatusDto
{
    public string Status { get; init; } = default!;
    public string? Notes { get; init; }
}

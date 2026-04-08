using App.Domain.Identity;

namespace App.Domain.Venues;

public class VenueAccessRequest : BaseEntity
{
    public Guid RequestorUserId { get; set; }
    public AppUser RequestorUser { get; set; } = default!;
    public Guid? ReviewedByUserId { get; set; }
    public AppUser? ReviewedByUser { get; set; }
    public Guid? CompanyId { get; set; }
    public Company? Company { get; set; }
    public Guid? VenueId { get; set; }
    public Venue? Venue { get; set; }

    public string CompanyName { get; set; } = default!;
    public string VenueName { get; set; } = default!;
    public string ContactName { get; set; } = default!;
    public string ContactEmail { get; set; } = default!;
    public string? ContactPhone { get; set; }
    public string City { get; set; } = default!;
    public string Country { get; set; } = default!;
    public string AddressLine1 { get; set; } = default!;
    public string? Notes { get; set; }
    public int EstimatedMonthlyBookings { get; set; }
    public VenueAccessRequestStatus Status { get; set; } = VenueAccessRequestStatus.PendingReview;
    public VenueAccessLevel? ApprovedAccessLevel { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNotes { get; set; }
}

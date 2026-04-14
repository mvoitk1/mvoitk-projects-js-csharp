namespace App.DTO.v1.Venues.Public;

public class SubmitVenueAccessRequestDto
{
    public string CompanyName { get; init; } = default!;
    public string VenueName { get; init; } = default!;
    public string ContactName { get; init; } = default!;
    public string ContactEmail { get; init; } = default!;
    public string? ContactPhone { get; init; }
    public string City { get; init; } = default!;
    public string Country { get; init; } = default!;
    public string AddressLine1 { get; init; } = default!;
    public int EstimatedMonthlyBookings { get; init; }
    public string? Notes { get; init; }
}

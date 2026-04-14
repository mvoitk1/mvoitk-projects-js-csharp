namespace App.DTO.v1.Venues.Employee;

public class BookingApprovalResultDto
{
    public Guid BookingId { get; init; }
    public string Status { get; init; } = default!;
    public DateTime ApprovedAt { get; init; }
}

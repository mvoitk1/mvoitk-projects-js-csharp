namespace App.Domain.Venues;

public enum BookingStatus
{
    Draft = 0,
    PendingApproval = 1,
    Confirmed = 2,
    InProgress = 3,
    Completed = 4,
    Cancelled = 5
}

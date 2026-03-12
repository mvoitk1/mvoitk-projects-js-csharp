namespace App.Domain.Venues;

public enum CateringOrderStatus
{
    Draft = 0,
    Submitted = 1,
    Confirmed = 2,
    Locked = 3,
    Fulfilled = 4,
    Cancelled = 5
}

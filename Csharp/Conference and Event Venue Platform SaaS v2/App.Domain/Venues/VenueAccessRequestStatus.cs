namespace App.Domain.Venues;

public enum VenueAccessRequestStatus
{
    PendingReview = 0,
    InReview = 1,
    Approved = 2,
    Rejected = 3,
    Deferred = 4
}

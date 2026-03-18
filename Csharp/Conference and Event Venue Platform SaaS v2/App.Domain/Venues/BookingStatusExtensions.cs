namespace App.Domain.Venues;

public static class BookingStatusExtensions
{
    public static bool IsEditable(this BookingStatus status) =>
        status is BookingStatus.Draft or BookingStatus.PendingApproval or BookingStatus.Confirmed;

    public static bool AppearsOnCalendar(this BookingStatus status) =>
        status is BookingStatus.Confirmed or BookingStatus.InProgress or BookingStatus.Completed;
}

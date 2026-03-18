namespace WebApp.ViewModels.Workspace;

public class BookingCalendarItemViewModel
{
    public Guid BookingId { get; init; }
    public string Title { get; init; } = default!;
    public string PrimaryLabel { get; init; } = default!;
    public string SecondaryLabel { get; init; } = default!;
    public string Status { get; init; } = default!;
    public DateTime StartsAt { get; init; }
    public DateTime EndsAt { get; init; }
    public int ExpectedAttendees { get; init; }
    public string LinkArea { get; init; } = string.Empty;
    public string LinkController { get; init; } = default!;
    public string LinkAction { get; init; } = default!;
    public string LinkText { get; init; } = default!;
}

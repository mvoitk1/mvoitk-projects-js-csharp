namespace WebApp.ViewModels.Workspace;

public class BookingCalendarSectionViewModel
{
    public string Title { get; init; } = default!;
    public string Description { get; init; } = default!;
    public string EmptyMessage { get; init; } = default!;
    public IReadOnlyList<BookingCalendarItemViewModel> Bookings { get; init; } = [];
}

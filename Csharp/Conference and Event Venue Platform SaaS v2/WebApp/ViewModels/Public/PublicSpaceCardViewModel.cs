namespace WebApp.ViewModels.Public;

public class PublicSpaceCardViewModel
{
    public Guid SpaceId { get; init; }
    public string Name { get; init; } = default!;
    public string Code { get; init; } = default!;
    public string? Description { get; init; }
    public string Status { get; init; } = default!;
    public string HourlyRate { get; init; } = default!;
    public int MaximumCapacity { get; init; }
    public int MinimumBookingDurationMinutes { get; init; }
    public IReadOnlyList<PublicSpaceLayoutCardViewModel> Layouts { get; init; } = [];
}

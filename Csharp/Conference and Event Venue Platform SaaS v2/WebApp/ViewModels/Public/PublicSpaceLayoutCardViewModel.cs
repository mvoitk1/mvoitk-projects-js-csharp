namespace WebApp.ViewModels.Public;

public class PublicSpaceLayoutCardViewModel
{
    public Guid LayoutId { get; init; }
    public string Name { get; init; } = default!;
    public string LayoutType { get; init; } = default!;
    public int Capacity { get; init; }
    public bool IsDefault { get; init; }
    public string? Notes { get; init; }
}

namespace App.DTO.v1.Venues.Public;

public class PublicSpaceLayoutOptionDto
{
    public Guid LayoutId { get; init; }
    public string Name { get; init; } = default!;
    public string LayoutType { get; init; } = default!;
    public int Capacity { get; init; }
    public bool IsDefault { get; init; }
    public string? Notes { get; init; }
}

namespace App.Domain.Venues;

public class SpaceLayout : BaseEntity
{
    public Guid SpaceId { get; set; }
    public Space Space { get; set; } = default!;

    public string Name { get; set; } = default!;
    public LayoutType LayoutType { get; set; }
    public int Capacity { get; set; }
    public bool IsDefault { get; set; }
    public string? Notes { get; set; }
}

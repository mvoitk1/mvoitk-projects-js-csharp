using Base.Domain;
using Sales.Domain.Enums;

namespace Sales.Domain;

public class Cart : DomainEntityId
{
    public CartStatus Status { get; set; } = CartStatus.Active;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Owned by Users module — stored as plain Guid (no FK constraint across modules).
    public Guid AppUserId { get; set; }

    public ICollection<CartItem> Items { get; set; } = null!;
}

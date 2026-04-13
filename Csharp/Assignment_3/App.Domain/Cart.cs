using App.Domain.Enums;
using App.Domain.Identity;

namespace App.Domain;

public class Cart : BaseEntity
{
    public CartStatus Status { get; set; } = CartStatus.Active;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Guid AppUserId { get; set; }
    public AppUser? AppUser { get; set; }

    public ICollection<CartItem>? Items { get; set; }
}

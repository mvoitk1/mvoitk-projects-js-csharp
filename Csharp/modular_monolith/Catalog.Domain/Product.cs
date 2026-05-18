using System.ComponentModel.DataAnnotations.Schema;
using Base.Domain;
using Catalog.Domain.Enums;
using Modules.SharedKernel;

namespace Catalog.Domain;

public class Product : DomainEntityId
{
    [Column(TypeName = "jsonb")]
    public LangStr Name { get; set; } = new LangStr();

    [Column(TypeName = "jsonb")]
    public LangStr Description { get; set; } = new LangStr();

    [Column(TypeName = "jsonb")]
    public LangStr Material { get; set; } = new LangStr();

    public Gender Gender { get; set; } = Gender.NotSpecified;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid? CollectionId { get; set; }
    public Collection? Collection { get; set; }

    public ICollection<ProductVariant> Variants { get; set; } = null!;
    public ICollection<ProductImage> Images { get; set; } = null!;
    public ICollection<ProductCategory> ProductCategories { get; set; } = null!;
}

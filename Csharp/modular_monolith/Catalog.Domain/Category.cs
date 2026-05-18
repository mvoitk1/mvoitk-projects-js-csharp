using System.ComponentModel.DataAnnotations.Schema;
using Base.Domain;
using Modules.SharedKernel;

namespace Catalog.Domain;

public class Category : DomainEntityId
{
    [Column(TypeName = "jsonb")]
    public LangStr Name { get; set; } = new LangStr();

    public Guid? ParentCategoryId { get; set; }
    public Category? ParentCategory { get; set; }

    public ICollection<Category> SubCategories { get; set; } = null!;
    public ICollection<ProductCategory> ProductCategories { get; set; } = null!;
}

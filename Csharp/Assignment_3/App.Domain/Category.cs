using System.ComponentModel.DataAnnotations.Schema;

namespace App.Domain;

public class Category : BaseEntity
{
    [Column(TypeName = "jsonb")]
    public LangStr Name { get; set; } = new LangStr();

    public Guid? ParentCategoryId { get; set; }
    public Category? ParentCategory { get; set; }

    public ICollection<Category>? SubCategories { get; set; }
    public ICollection<ProductCategory>? ProductCategories { get; set; }
}

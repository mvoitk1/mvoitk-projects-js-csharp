using System.ComponentModel.DataAnnotations.Schema;

namespace App.Domain;

public class ProductImage : BaseEntity
{
    public string Url { get; set; } = string.Empty;

    [Column(TypeName = "jsonb")]
    public LangStr AltText { get; set; } = new LangStr();

    public int SortOrder { get; set; } = 0;

    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
}

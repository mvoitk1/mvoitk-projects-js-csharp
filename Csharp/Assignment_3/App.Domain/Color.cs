using System.ComponentModel.DataAnnotations.Schema;

namespace App.Domain;

public class Color : BaseEntity
{
    [Column(TypeName = "jsonb")]
    public LangStr Name { get; set; } = new LangStr();

    public string HexCode { get; set; } = string.Empty;

    public ICollection<ProductVariant>? ProductVariants { get; set; }
}

using Base.Domain;
using System.ComponentModel.DataAnnotations.Schema;

namespace App.Domain;

public class Size : DomainEntityId
{
    public string SizeCode { get; set; } = string.Empty;

    [Column(TypeName = "jsonb")]
    public LangStr DisplayName { get; set; } = new LangStr();

    public ICollection<ProductVariant> ProductVariants { get; set; } = null!;
}

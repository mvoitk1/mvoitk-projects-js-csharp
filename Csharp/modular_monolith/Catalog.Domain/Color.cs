using System.ComponentModel.DataAnnotations.Schema;
using Base.Domain;
using Modules.SharedKernel;

namespace Catalog.Domain;

public class Color : DomainEntityId
{
    [Column(TypeName = "jsonb")]
    public LangStr Name { get; set; } = new LangStr();

    public string HexCode { get; set; } = string.Empty;

    public ICollection<ProductVariant> ProductVariants { get; set; } = null!;
}

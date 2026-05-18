using System.ComponentModel.DataAnnotations.Schema;
using Base.Domain;
using Modules.SharedKernel;

namespace Catalog.Domain;

public class ProductImage : DomainEntityId
{
    public string Url { get; set; } = string.Empty;

    [Column(TypeName = "jsonb")]
    public LangStr AltText { get; set; } = new LangStr();

    public int SortOrder { get; set; } = 0;

    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
}

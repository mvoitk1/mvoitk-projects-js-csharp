using System.ComponentModel.DataAnnotations.Schema;
using Base.Domain;
using Modules.SharedKernel;

namespace Catalog.Domain;

public class Collection : DomainEntityId
{
    [Column(TypeName = "jsonb")]
    public LangStr Name { get; set; } = new LangStr();

    [Column(TypeName = "jsonb")]
    public LangStr Description { get; set; } = new LangStr();

    public DateTime? LaunchDate { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Product> Products { get; set; } = null!;
}

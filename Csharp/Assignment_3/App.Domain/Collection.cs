using System.ComponentModel.DataAnnotations.Schema;

namespace App.Domain;

public class Collection : BaseEntity
{
    [Column(TypeName = "jsonb")]
    public LangStr Name { get; set; } = new LangStr();

    [Column(TypeName = "jsonb")]
    public LangStr Description { get; set; } = new LangStr();

    public DateTime? LaunchDate { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Product> Products { get; set; } = null!;
}

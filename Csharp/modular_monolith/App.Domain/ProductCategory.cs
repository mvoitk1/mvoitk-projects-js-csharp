namespace App.Domain;

public class ProductCategory
{
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }

    public Guid CategoryId { get; set; }
    public Category? Category { get; set; }

    public DateTime? From { get; set; }
    public DateTime? Until { get; set; }
}

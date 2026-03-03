namespace VenuePlatform.BLL.Domain.Companies;

public sealed class Company
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string Slug { get; private set; } = null!;
    public DateTime CreatedUtc { get; private set; }

    private Company() { } // EF

    public Company(string name, string slug)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(slug)) throw new ArgumentException("Slug is required.", nameof(slug));

        Id = Guid.NewGuid();
        Name = name.Trim();
        Slug = slug.Trim().ToLowerInvariant();
        CreatedUtc = DateTime.UtcNow;
    }
}

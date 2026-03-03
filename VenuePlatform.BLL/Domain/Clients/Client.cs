using VenuePlatform.Contracts.Tenancy;

namespace VenuePlatform.BLL.Domain.Clients;

public sealed class Client : ITenantScoped
{
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Notes { get; private set; }
    public DateTime CreatedUtc { get; private set; }

    private Client() { } // EF

    public Client(Guid companyId, string name, string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));

        Id = Guid.NewGuid();
        CompanyId = companyId;
        Name = name.Trim();
        Notes = notes;
        CreatedUtc = DateTime.UtcNow;
    }
}

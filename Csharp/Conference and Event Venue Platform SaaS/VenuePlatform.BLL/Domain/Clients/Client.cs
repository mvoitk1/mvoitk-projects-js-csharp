// Client - A customer/contact who books spaces at the venue
using VenuePlatform.Contracts.Tenancy;

namespace VenuePlatform.BLL.Domain.Clients;

/// <summary>
/// Client entity - represents a customer who can book spaces.
/// Could be an individual or a company/organization.
/// </summary>
public sealed class Client : ITenantScoped
{
    public Guid Id { get; private set; }          // Unique ID
    public Guid CompanyId { get; private set; }   // Venue company this belongs to
    public string Name { get; private set; } = null!;  // Client name
    public string? Email { get; private set; }   // Contact email (optional)
    public string? Notes { get; private set; }   // Internal notes (optional)
    public DateTime CreatedUtc { get; private set; }  // When created

    // Required for Entity Framework Core
    private Client() { }

    // Constructor - creates a new client
    public Client(Guid companyId, string name, string? notes = null)
    {
        // Validate name
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));

        Id = Guid.NewGuid();
        CompanyId = companyId;
        Name = name.Trim();
        Notes = notes;
        CreatedUtc = DateTime.UtcNow;
    }

    // Update client's email
    public void SetEmail(string? email)
    {
        Email = email;
    }

    // Update client's name
    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));
        Name = name.Trim();
    }

    // Update notes
    public void SetNotes(string? notes)
    {
        Notes = notes;
    }

    // Update email (alternative method name)
    public void UpdateEmail(string? email)
    {
        Email = email;
    }
}

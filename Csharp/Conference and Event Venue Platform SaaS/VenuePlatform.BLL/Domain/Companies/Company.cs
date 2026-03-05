// Company - A venue/business in the platform (the tenant)
using VenuePlatform.Contracts.Billing;

namespace VenuePlatform.BLL.Domain.Companies;

/// <summary>
/// Company entity - represents a venue/business in the system.
/// Also known as "Tenant" in multi-tenant architecture.
/// </summary>
public sealed class Company
{
    public Guid Id { get; private set; }           // Unique ID
    public string Name { get; private set; } = null!;  // Display name
    public string Slug { get; private set; } = null!;  // URL-friendly identifier (e.g., "my-venue")
    public DateTime CreatedUtc { get; private set; }  // When created
    public CompanyPlan Plan { get; private set; }   // Subscription plan

    // Required for Entity Framework Core
    private Company() { }

    // Constructor - creates a new company with Free plan
    public Company(string name, string slug)
    {
        // Validate inputs
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(slug)) throw new ArgumentException("Slug is required.", nameof(slug));

        Id = Guid.NewGuid();
        Name = name.Trim();
        Slug = slug.Trim().ToLowerInvariant();  // Convert to lowercase
        CreatedUtc = DateTime.UtcNow;
        Plan = CompanyPlan.Free;  // Default plan for new companies
    }

    // Update subscription plan
    public void SetPlan(CompanyPlan plan)
    {
        Plan = plan;
    }
}

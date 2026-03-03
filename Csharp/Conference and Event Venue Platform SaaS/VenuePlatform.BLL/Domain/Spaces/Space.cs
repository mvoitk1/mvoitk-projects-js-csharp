using VenuePlatform.Contracts.Tenancy;

namespace VenuePlatform.BLL.Domain.Spaces;

public sealed class Space : ITenantScoped
{
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public string Name { get; private set; } = null!;
    public int Capacity { get; private set; }
    public decimal HourlyRate { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedUtc { get; private set; }

    private Space() { } // EF

    public Space(Guid companyId, string name, int capacity, decimal hourlyRate)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));
        if (capacity <= 0)
            throw new ArgumentException("Capacity must be greater than 0.", nameof(capacity));
        if (hourlyRate < 0)
            throw new ArgumentException("Hourly rate cannot be negative.", nameof(hourlyRate));

        Id = Guid.NewGuid();
        CompanyId = companyId;
        Name = name.Trim();
        Capacity = capacity;
        HourlyRate = hourlyRate;
        IsActive = true;
        CreatedUtc = DateTime.UtcNow;
    }

    public void Deactivate() => IsActive = false;
}

using VenuePlatform.Contracts.Tenancy;

namespace VenuePlatform.BLL.Domain.Spaces;

public sealed class Space : ITenantScoped
{
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public string Name { get; private set; } = null!;
    public int Capacity { get; private set; }
    public decimal HourlyRate { get; private set; }
    public string? Notes { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedUtc { get; private set; }

    private Space() { } // EF

    public Space(Guid companyId, string name, int capacity, decimal hourlyRate, string? notes = null)
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
        Notes = notes;
        IsActive = true;
        CreatedUtc = DateTime.UtcNow;
    }

    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));
        Name = name.Trim();
    }

    public void UpdateCapacity(int capacity)
    {
        if (capacity <= 0)
            throw new ArgumentException("Capacity must be greater than 0.", nameof(capacity));
        Capacity = capacity;
    }

    public void UpdateHourlyRate(decimal hourlyRate)
    {
        if (hourlyRate < 0)
            throw new ArgumentException("Hourly rate cannot be negative.", nameof(hourlyRate));
        HourlyRate = hourlyRate;
    }

    public void SetNotes(string? notes) => Notes = notes;

    public void Deactivate() => IsActive = false;
}

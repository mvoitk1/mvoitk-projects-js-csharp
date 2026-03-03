using VenuePlatform.Contracts.Tenancy;

namespace VenuePlatform.BLL.Domain.Spaces;

public sealed class SpaceConfiguration : ITenantScoped
{
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public string Name { get; private set; } = null!;
    public decimal? HourlyRateOverride { get; private set; }
    public int? MinBookingMinutesOverride { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedUtc { get; private set; }

    private SpaceConfiguration() { } // EF

    public SpaceConfiguration(Guid companyId, string name, decimal? hourlyRateOverride = null, int? minBookingMinutesOverride = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));
        if (hourlyRateOverride.HasValue && hourlyRateOverride.Value < 0)
            throw new ArgumentException("Hourly rate override cannot be negative.", nameof(hourlyRateOverride));
        if (minBookingMinutesOverride.HasValue && minBookingMinutesOverride.Value <= 0)
            throw new ArgumentException("Minimum booking minutes override must be greater than 0.", nameof(minBookingMinutesOverride));

        Id = Guid.NewGuid();
        CompanyId = companyId;
        Name = name.Trim();
        HourlyRateOverride = hourlyRateOverride;
        MinBookingMinutesOverride = minBookingMinutesOverride;
        IsActive = true;
        CreatedUtc = DateTime.UtcNow;
    }

    public void Deactivate() => IsActive = false;
}

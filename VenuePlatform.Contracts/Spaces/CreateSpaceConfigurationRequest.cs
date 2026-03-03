namespace VenuePlatform.Contracts.Spaces;

public sealed record CreateSpaceConfigurationRequest(
    string Name,
    decimal? HourlyRateOverride,
    int? MinBookingMinutesOverride);

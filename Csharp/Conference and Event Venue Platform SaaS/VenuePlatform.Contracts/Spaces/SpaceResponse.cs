namespace VenuePlatform.Contracts.Spaces;

public sealed record SpaceResponse(Guid Id, Guid CompanyId, string Name, int Capacity, decimal HourlyRate, string? Notes, bool IsActive);

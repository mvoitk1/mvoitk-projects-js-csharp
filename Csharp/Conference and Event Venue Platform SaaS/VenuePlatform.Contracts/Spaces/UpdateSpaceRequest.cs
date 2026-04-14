namespace VenuePlatform.Contracts.Spaces;

public sealed record UpdateSpaceRequest(string Name, int Capacity, decimal HourlyRate, string? Notes = null);

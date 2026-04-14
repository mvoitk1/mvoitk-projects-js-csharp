namespace VenuePlatform.Contracts.Spaces;

public sealed record CreateSpaceRequest(string Name, int Capacity, decimal HourlyRate, string? Notes = null);

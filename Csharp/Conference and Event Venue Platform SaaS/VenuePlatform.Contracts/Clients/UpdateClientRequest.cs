namespace VenuePlatform.Contracts.Clients;

/// <summary>
/// Request to update an existing client.
/// </summary>
public sealed record UpdateClientRequest(
    string Name,
    string? Notes = null,
    string? Email = null
);

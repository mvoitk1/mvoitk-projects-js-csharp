namespace VenuePlatform.Contracts.Clients;

/// <summary>
/// Request to create a new client.
/// </summary>
public sealed record CreateClientRequest(
    string Name,
    string? Notes = null,
    string? Email = null
);

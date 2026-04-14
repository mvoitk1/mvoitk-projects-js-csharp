namespace VenuePlatform.Contracts.Clients;

/// <summary>
/// Response containing client details.
/// </summary>
public sealed record ClientResponse(
    Guid Id,
    Guid CompanyId,
    string Name,
    string? Notes,
    string? Email,
    DateTime CreatedUtc
);

namespace VenuePlatform.Contracts.Spaces;

public sealed record PublicSpaceDto(
    Guid Id,
    string Name,
    int Capacity,
    string? Notes
);

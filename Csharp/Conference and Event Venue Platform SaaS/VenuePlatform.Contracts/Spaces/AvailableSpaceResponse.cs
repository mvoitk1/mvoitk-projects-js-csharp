namespace VenuePlatform.Contracts.Spaces;

public sealed record AvailableSpaceResponse(
    Guid Id,
    string Name,
    int Capacity);

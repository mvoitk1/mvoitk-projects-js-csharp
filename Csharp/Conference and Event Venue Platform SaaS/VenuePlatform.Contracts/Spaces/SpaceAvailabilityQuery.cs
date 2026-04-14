namespace VenuePlatform.Contracts.Spaces;

public sealed record SpaceAvailabilityQuery(
    DateTime StartUtc,
    DateTime EndUtc,
    int? MinCapacity = null,
    bool OnlyActive = true);

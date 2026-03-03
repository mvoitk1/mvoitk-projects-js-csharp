namespace VenuePlatform.Contracts.Spaces;

public sealed record SetSpaceConfigurationSpacesRequest(IReadOnlyList<Guid> SpaceIds);

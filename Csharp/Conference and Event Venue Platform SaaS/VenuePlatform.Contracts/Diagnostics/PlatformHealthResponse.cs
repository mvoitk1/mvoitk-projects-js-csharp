namespace VenuePlatform.Contracts.Diagnostics;

public sealed record PlatformHealthResponse(
    DateTime UtcNow,
    string Environment,
    bool DatabaseOk
);

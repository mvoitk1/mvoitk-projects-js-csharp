namespace VenuePlatform.Contracts.Auth;

/// <summary>
/// Request to register a new user account.
/// Supports two modes:
/// - Owner: Creates a new company and assigns the user as CompanyOwner.
/// - User: Joins an existing company with Employee role.
/// </summary>
public sealed record RegisterRequest(
    string Email,
    string Password,
    string Mode, // "Owner" or "User"
    string? CompanyName,    // Required when Mode == "Owner"
    string? CompanySlug,    // Required for both modes
    string? Role            // Optional, only honored internally (default: Employee)
);

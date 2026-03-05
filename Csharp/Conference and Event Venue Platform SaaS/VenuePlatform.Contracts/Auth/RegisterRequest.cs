namespace VenuePlatform.Contracts.Auth;

/// <summary>
/// Request to register a new user account.
/// Supports three modes:
/// - Mode omitted/null: Creates user account only (no company, no membership)
/// - Mode == "Owner": Creates a new company and assigns the user as CompanyOwner.
/// - Mode == "User": Joins an existing company with Employee role.
/// </summary>
public sealed record RegisterRequest(
    string Email,
    string Password,
    string? Mode = null,               // "Owner", "User", or null for account-only signup
    string? CompanyName = null,        // Required when Mode == "Owner"
    string? CompanySlug = null,        // Required when Mode == "Owner" or Mode == "User"
    string? Role = null                // Optional, only honored internally (default: Employee)
);

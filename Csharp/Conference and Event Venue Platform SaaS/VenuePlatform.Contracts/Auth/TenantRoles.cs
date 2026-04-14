namespace VenuePlatform.Contracts.Auth;

public static class TenantRoles
{
    // Company-scoped roles
    public const string CompanyOwner = "CompanyOwner";
    public const string CompanyAdmin = "CompanyAdmin";
    public const string CompanyManager = "CompanyManager";
    public const string CompanyEmployee = "CompanyEmployee";

    // Platform-level roles
    public const string PlatformAdmin = "PlatformAdmin";
}

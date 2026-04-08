namespace App.Domain.Identity;

public static class AppRoles
{
    public const string User = "User";
    public const string CompanyEmployee = "CompanyEmployee";
    public const string CompanyManager = "CompanyManager";
    public const string Admin = "Admin";

    public static IReadOnlyList<string> All { get; } =
    [
        User,
        CompanyEmployee,
        CompanyManager,
        Admin
    ];

    public static IReadOnlyList<string> VenueOperators { get; } =
    [
        CompanyEmployee,
        CompanyManager
    ];
}

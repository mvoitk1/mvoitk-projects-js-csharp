namespace App.DAL.EF.Seeding;

public static class InitialData
{
    public static readonly (string roleName, Guid? id)[]
        Roles =
        [
            ("Admin", null),
            ("Customer", null),
        ];

    public static readonly (string name, string password, Guid? id, string[] roles)[]
        Users =
        [
            ("admin@shop.ee", "Admin.Password.1", null, ["Admin", "Customer"]),
        ];
}

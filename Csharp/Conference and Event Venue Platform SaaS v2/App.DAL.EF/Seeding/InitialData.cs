using App.Domain.Identity;

namespace App.DAL.EF.Seeding;

public static class InitialData
{
    public static readonly (string roleName, Guid? id)[]
        Roles =
        [
            (AppRoles.Admin, null),
            (AppRoles.User, null),
            (AppRoles.CompanyEmployee, null),
            (AppRoles.CompanyManager, null),
        ];

    public static readonly (string name, string password, Guid? id, string[] roles)[]
        Users =
        [
            ("akaver@akaver.com", "Kala.Maja.101", null, [AppRoles.Admin, AppRoles.User]),
            ("andres.kaver@taltech.ee", "Kala.Maja.101", null, [AppRoles.Admin, AppRoles.CompanyManager, AppRoles.User]),
            ("manager@northstarvenues.test", "Kala.Maja.101", null, [AppRoles.CompanyManager, AppRoles.User]),
            ("employee@northstarvenues.test", "Kala.Maja.101", null, [AppRoles.CompanyEmployee, AppRoles.User]),
            ("requester@newvenue.test", "Kala.Maja.101", null, [AppRoles.User]),
        ];
}

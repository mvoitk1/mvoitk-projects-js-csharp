using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Users.Domain;

namespace Users.Infrastructure.Seeding;

public static class UsersDataInit
{
    public static readonly (string roleName, Guid? id)[] Roles =
    [
        ("Admin", null),
        ("Customer", null),
    ];

    public static readonly (string name, string password, Guid? id, string[] roles)[] Users =
    [
        ("admin@shop.ee", "Admin.Password.1", null, ["Admin", "Customer"]),
    ];

    public static void MigrateDatabase(UsersDbContext context) => context.Database.Migrate();

    public static void DeleteDatabase(UsersDbContext context) => context.Database.EnsureDeleted();

    public static async Task SeedIdentityAsync(UserManager<AppUser> userManager, RoleManager<AppRole> roleManager)
    {
        foreach (var (roleName, _) in Roles)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role != null) continue;

            role = new AppRole { Name = roleName };
            var result = await roleManager.CreateAsync(role);
            if (!result.Succeeded)
                throw new ApplicationException("Role creation failed!");
        }

        foreach (var userInfo in Users)
        {
            var user = await userManager.FindByEmailAsync(userInfo.name);
            if (user == null)
            {
                user = new AppUser
                {
                    Email = userInfo.name,
                    UserName = userInfo.name,
                    EmailConfirmed = true,
                    FirstName = "Admin",
                    LastName = "User",
                    CreatedAt = DateTime.UtcNow
                };
                var result = await userManager.CreateAsync(user, userInfo.password);
                if (!result.Succeeded)
                    throw new ApplicationException("User creation failed!");
            }

            foreach (var role in userInfo.roles)
            {
                if (await userManager.IsInRoleAsync(user, role)) continue;
                await userManager.AddToRoleAsync(user, role);
            }
        }
    }
}

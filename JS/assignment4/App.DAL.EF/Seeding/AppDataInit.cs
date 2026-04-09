using System;
using System.Linq;
using App.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.EF.Seeding
{
    public static class AppDataInit
    {
        public static void SeedAppData(AppDbContext context)
        {
            // Seed API keys for ListItems integration tests
            var adminUser = context.Users.FirstOrDefault(u => u.Email == InitialData.Users[0].name);
            if (adminUser != null && !context.ApiKeys.Any())
            {
                context.ApiKeys.Add(new App.Domain.ApiKey.ApiKey
                {
                    SecretKey = InitialData.ApiKey1Secret,
                    AppName = "TestApp1",
                    IsDisabled = false,
                    AppUserId = adminUser.Id
                });
                context.ApiKeys.Add(new App.Domain.ApiKey.ApiKey
                {
                    SecretKey = InitialData.ApiKey2Secret,
                    AppName = "TestApp2",
                    IsDisabled = false,
                    AppUserId = adminUser.Id
                });
                context.SaveChanges();
            }
        }

        public static void MigrateDatabase(AppDbContext context)
        {
            context.Database.Migrate();
        }

        public static void DeleteDatabase(AppDbContext context)
        {
            context.Database.EnsureDeleted();
        }
        
        public static void SeedIdentity(UserManager<AppUser> userManager, RoleManager<AppRole> roleManager)
        {
            foreach (var (roleName, roleDisplayName, id) in InitialData.Roles)
            {
                var role = roleManager.FindByNameAsync(roleName).Result;
                if (role == null)
                {
                    role = new AppRole()
                    {
                        //Id = id,
                        Name = roleName,
                        DisplayName = roleDisplayName,
                    };

                    var result = roleManager.CreateAsync(role).Result;
                    if (!result.Succeeded)
                    {
                        throw new ApplicationException("Role creation failed!");
                    }
                }
            }


            foreach (var userInfo in InitialData.Users)
            {
                var user = userManager.FindByEmailAsync(userInfo.name).Result;
                if (user == null)
                {
                    user = new AppUser()
                    {
                        // Id = userInfo.id,
                        Email = userInfo.name,
                        UserName = userInfo.name,
                        FirstName = userInfo.firstName,
                        LastName = userInfo.lastName,
                        EmailConfirmed = true
                    };
                    var result = userManager.CreateAsync(user, userInfo.password).Result;
                    if (!result.Succeeded)
                    {
                        throw new ApplicationException("User creation failed!");
                    }
                }

                if (userInfo.role != "")
                {
                    var roleResult = userManager.AddToRoleAsync(user, userInfo.role).Result;
                }
            }
        }

    }
}
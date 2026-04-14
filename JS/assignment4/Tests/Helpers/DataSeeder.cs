using App.DAL.EF;
using App.DAL.EF.Seeding;
using App.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace WebApp.Tests.Helpers;

public static class DataSeeder
{
    public static void SeedData(AppDbContext ctx)
    {
        // Note: Identity data is seeded separately via SeedIdentityData
        // because UserManager and RoleManager are needed
        AppDataInit.SeedAppData(ctx);
    }
    
    public static void SeedIdentityData(IServiceProvider serviceProvider)
    {
        var userManager = serviceProvider.GetRequiredService<UserManager<AppUser>>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<AppRole>>();
        AppDataInit.SeedIdentity(userManager, roleManager);
    }
}
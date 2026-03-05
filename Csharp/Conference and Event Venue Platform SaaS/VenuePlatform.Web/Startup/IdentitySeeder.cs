using Microsoft.AspNetCore.Identity;
using VenuePlatform.Contracts.Auth;

namespace VenuePlatform.Web.Startup;

/// <summary>
/// Seeds the platform admin user and role at application startup.
/// Only runs when configured (disabled by default in production).
/// </summary>
public static class IdentitySeeder
{
    /// <summary>
    /// Seeds the platform admin user if enabled and not already present.
    /// </summary>
    public static async Task SeedPlatformAdminAsync(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger logger)
    {
        var seedConfig = configuration.GetSection("SeedAdmin");
        var isEnabled = seedConfig.GetValue<bool?>("Enabled") ?? false;

        if (!isEnabled)
        {
            logger.LogInformation("Platform admin seeding is disabled.");
            return;
        }

        var email = seedConfig["Email"] ?? "admin@venueplatform.local";
        var password = seedConfig["Password"];

        // Safety: require explicit password in non-development environments
        if (string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("Platform admin seeding is enabled but no password is configured. Skipping.");
            return;
        }

        using var scope = serviceProvider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser<Guid>>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        // Ensure PlatformAdmin role exists
        if (!await roleManager.RoleExistsAsync(TenantRoles.PlatformAdmin))
        {
            logger.LogInformation("Creating PlatformAdmin role...");
            await roleManager.CreateAsync(new IdentityRole<Guid>(TenantRoles.PlatformAdmin));
        }

        // Check if admin user already exists
        var existingUser = await userManager.FindByEmailAsync(email);
        if (existingUser is not null)
        {
            logger.LogInformation("Platform admin user '{Email}' already exists. Skipping seed.", email);

            // Ensure role is assigned even if user exists
            if (!await userManager.IsInRoleAsync(existingUser, TenantRoles.PlatformAdmin))
            {
                logger.LogInformation("Assigning PlatformAdmin role to existing user...");
                await userManager.AddToRoleAsync(existingUser, TenantRoles.PlatformAdmin);
            }

            return;
        }

        // Create the admin user
        logger.LogInformation("Creating platform admin user '{Email}'...", email);

        var adminUser = new IdentityUser<Guid>
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            EmailConfirmed = true
        };

        var createResult = await userManager.CreateAsync(adminUser, password);
        if (!createResult.Succeeded)
        {
            var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
            logger.LogError("Failed to create platform admin user: {Errors}", errors);
            return;
        }

        // Assign PlatformAdmin role
        var roleResult = await userManager.AddToRoleAsync(adminUser, TenantRoles.PlatformAdmin);
        if (!roleResult.Succeeded)
        {
            var errors = string.Join("; ", roleResult.Errors.Select(e => e.Description));
            logger.LogError("Failed to assign PlatformAdmin role: {Errors}", errors);
            return;
        }

        logger.LogInformation("Platform admin user '{Email}' created successfully.", email);
    }
}

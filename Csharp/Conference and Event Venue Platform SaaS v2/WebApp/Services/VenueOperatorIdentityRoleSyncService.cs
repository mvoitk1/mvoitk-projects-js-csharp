using App.DAL.EF;
using App.Domain.Identity;
using App.Domain.Venues;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace WebApp.Services;

public class VenueOperatorIdentityRoleSyncService(
    AppDbContext context,
    UserManager<AppUser> userManager) : IVenueOperatorIdentityRoleSyncService
{
    public async Task SyncUserRolesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            throw new KeyNotFoundException("User was not found.");
        }

        var activeAccessLevels = await context.VenueMemberships
            .AsNoTracking()
            .Where(item => item.UserId == userId && item.Status == VenueMembershipStatus.Active)
            .Select(item => item.AccessLevel)
            .Distinct()
            .ToListAsync(cancellationToken);

        var shouldHaveManagerRole = activeAccessLevels.Contains(VenueAccessLevel.Manager);
        var shouldHaveEmployeeRole = activeAccessLevels.Contains(VenueAccessLevel.Employee);

        await SyncRoleAsync(user, AppRoles.CompanyManager, shouldHaveManagerRole);
        await SyncRoleAsync(user, AppRoles.CompanyEmployee, shouldHaveEmployeeRole);
    }

    private async Task SyncRoleAsync(AppUser user, string roleName, bool shouldHaveRole)
    {
        var isInRole = await userManager.IsInRoleAsync(user, roleName);
        if (isInRole == shouldHaveRole)
        {
            return;
        }

        var result = shouldHaveRole
            ? await userManager.AddToRoleAsync(user, roleName)
            : await userManager.RemoveFromRoleAsync(user, roleName);

        if (!result.Succeeded)
        {
            var errorMessage = string.Join("; ", result.Errors.Select(error => error.Description));
            throw new InvalidOperationException(
                $"Failed to synchronize the '{roleName}' role for user '{user.Id}': {errorMessage}");
        }
    }
}

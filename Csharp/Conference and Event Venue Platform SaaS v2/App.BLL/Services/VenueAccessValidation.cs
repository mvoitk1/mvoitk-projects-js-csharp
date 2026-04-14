using App.DAL.EF;
using App.Domain.Identity;
using App.Domain.Venues;
using Microsoft.EntityFrameworkCore;

namespace App.BLL.Services;

internal static class VenueAccessValidation
{
    public static async Task<VenueMembership> RequireOperationalMembershipAsync(
        AppDbContext context,
        Guid userId,
        Guid venueId,
        CancellationToken cancellationToken)
    {
        var membership = await context.VenueMemberships
            .Include(item => item.Company)
            .Include(item => item.Venue)
            .SingleOrDefaultAsync(
                item => item.UserId == userId &&
                        item.VenueId == venueId,
                cancellationToken);

        if (membership == null || !membership.Status.CanOperate())
        {
            throw new InvalidOperationException("The user does not have active access to this venue.");
        }

        return membership;
    }

    public static async Task EnsureManagerAccessAsync(
        AppDbContext context,
        Guid userId,
        Guid venueId,
        CancellationToken cancellationToken)
    {
        if (await UserHasRoleAsync(context, userId, AppRoles.Admin, cancellationToken))
        {
            return;
        }

        var membership = await RequireOperationalMembershipAsync(context, userId, venueId, cancellationToken);
        if (membership.AccessLevel != VenueAccessLevel.Manager)
        {
            throw new InvalidOperationException("The user does not have manager access to this venue.");
        }
    }

    public static Task<bool> UserHasRoleAsync(
        AppDbContext context,
        Guid userId,
        string roleName,
        CancellationToken cancellationToken) =>
        (from userRole in context.UserRoles
            join role in context.Roles on userRole.RoleId equals role.Id
            where userRole.UserId == userId && role.Name == roleName
            select role.Id)
        .AnyAsync(cancellationToken);
}

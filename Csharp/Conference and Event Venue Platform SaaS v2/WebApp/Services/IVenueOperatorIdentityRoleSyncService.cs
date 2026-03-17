namespace WebApp.Services;

public interface IVenueOperatorIdentityRoleSyncService
{
    Task SyncUserRolesAsync(Guid userId, CancellationToken cancellationToken = default);
}

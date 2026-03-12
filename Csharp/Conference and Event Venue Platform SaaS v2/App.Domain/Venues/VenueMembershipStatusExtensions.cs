namespace App.Domain.Venues;

public static class VenueMembershipStatusExtensions
{
    public static bool CanOperate(this VenueMembershipStatus status) =>
        status == VenueMembershipStatus.Active;

    public static bool BlocksVenueSelection(this VenueMembershipStatus status) =>
        status is VenueMembershipStatus.PendingActivation or VenueMembershipStatus.Suspended or VenueMembershipStatus.Revoked;
}

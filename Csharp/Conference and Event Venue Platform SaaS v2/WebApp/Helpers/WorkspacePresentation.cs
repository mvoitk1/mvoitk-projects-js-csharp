using App.Resources.Views.Workspace;

namespace WebApp.Helpers;

public static class WorkspacePresentation
{
    public static string ToStatusLabel(string? value) => value?.ToLowerInvariant() switch
    {
        "draft" => Pages.StatusDraft,
        "pendingapproval" => Pages.StatusPendingApproval,
        "confirmed" => Pages.StatusConfirmed,
        "cancelled" => Pages.StatusCancelled,
        "pendingreview" => Pages.StatusPendingReview,
        "inreview" => Pages.StatusInReview,
        "approved" => Pages.StatusApproved,
        "rejected" => Pages.StatusRejected,
        "active" => Pages.StatusActive,
        "maintenance" => Pages.StatusMaintenance,
        "archived" => Pages.StatusArchived,
        "suspended" => Pages.StatusSuspended,
        "blocked" => Pages.StatusBlocked,
        _ => value ?? string.Empty
    };

    public static string ToBadgeClass(string? value) => value?.ToLowerInvariant() switch
    {
        "confirmed" or "approved" or "active" => "is-good",
        "pendingapproval" or "pendingreview" or "inreview" or "maintenance" => "is-warn",
        "cancelled" or "rejected" or "suspended" or "blocked" or "archived" => "is-bad",
        _ => "is-neutral"
    };

    public static string FormatSchedule(DateTime startsAt, DateTime endsAt) =>
        $"{startsAt:g} - {endsAt:t}";

    public static string FormatMoney(decimal amount, string currency) =>
        $"{amount:0.##} {currency}";
}

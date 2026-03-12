namespace WebApp.ViewModels.Workspace;

public class WorkspaceLayoutViewModel
{
    public WorkspaceContextViewModel Context { get; init; } = new();
    public string PageTitle { get; init; } = string.Empty;
    public string ActiveNavigation { get; init; } = string.Empty;
}

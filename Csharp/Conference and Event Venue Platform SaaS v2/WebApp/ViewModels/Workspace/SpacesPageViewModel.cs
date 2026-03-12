using App.DTO.v1.Venues.Admin;

namespace WebApp.ViewModels.Workspace;

public class SpacesPageViewModel
{
    public WorkspaceLayoutViewModel Layout { get; init; } = new();
    public IReadOnlyList<SpaceConfigurationDto> Spaces { get; init; } = [];
    public SpaceConfigurationDto? SelectedSpace { get; init; }
    public SpaceConfigurationFormViewModel Form { get; init; } = new();
}

namespace WebApp.Areas.Root.ViewModels;

public class PasswordLinkViewModel
{
    public string PasswordLink { get; set; } = default!;
    public string UserEmail { get; set; } = default!;
    public Guid UserId { get; set; }
}

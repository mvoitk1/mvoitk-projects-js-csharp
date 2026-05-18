using System.ComponentModel.DataAnnotations;

namespace Users.Application.Dtos.Identity;

public class LogoutInfo
{
    [MaxLength(128)]
    [Required]
    public string RefreshToken { get; set; } = default!;
}

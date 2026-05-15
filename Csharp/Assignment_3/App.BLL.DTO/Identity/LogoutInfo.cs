using System.ComponentModel.DataAnnotations;

namespace App.BLL.DTO.Identity;

public class LogoutInfo
{
    [MaxLength(128)]
    [Required]
    public string RefreshToken { get; set; } = default!;
}
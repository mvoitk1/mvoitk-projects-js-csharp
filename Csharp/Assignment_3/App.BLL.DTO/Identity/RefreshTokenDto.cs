using System.ComponentModel.DataAnnotations;

namespace App.BLL.DTO.Identity;

public class RefreshTokenDto
{
    [Required]
    public string Jwt { get; set; } = string.Empty;

    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}

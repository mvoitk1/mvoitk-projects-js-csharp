using System.ComponentModel.DataAnnotations;

namespace App.DTO.v1.Identity;

public class RefreshTokenDto
{
    [Required]
    public string Jwt { get; set; } = string.Empty;

    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}

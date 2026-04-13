using System.ComponentModel.DataAnnotations;

namespace App.DTO.v1.Identity;

public class RegisterDto
{
    [Required]
    [MinLength(1)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MinLength(1)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;
}

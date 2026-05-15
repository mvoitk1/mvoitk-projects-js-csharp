using System.ComponentModel.DataAnnotations;

namespace App.BLL.DTO.Identity;

public class Register
{
    [Required]
    [MaxLength(150)]
    public string FirstName { get; set; } = default!;

    [Required]
    [MaxLength(150)]
    public string LastName { get; set; } = default!;

    [MaxLength(256)]
    [EmailAddress]
    [Required]
    public string Email { get; set; } = default!;

    [MinLength(6)]
    [MaxLength(100)]
    [Required]
    public string Password { get; set; } = default!;
}
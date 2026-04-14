using System.ComponentModel.DataAnnotations;

namespace App.DTO.v1.Admin;

public class AdminOrderStatusDto
{
    [Required]
    public string Status { get; set; } = string.Empty;
}

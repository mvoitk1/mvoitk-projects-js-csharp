using System.ComponentModel.DataAnnotations;

namespace App.BLL.DTO.Admin;

public class AdminOrderStatusDto
{
    [Required]
    public string Status { get; set; } = string.Empty;
}

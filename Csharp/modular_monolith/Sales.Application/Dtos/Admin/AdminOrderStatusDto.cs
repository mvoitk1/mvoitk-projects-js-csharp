using System.ComponentModel.DataAnnotations;

namespace Sales.Application.Dtos.Admin;

public class AdminOrderStatusDto
{
    [Required]
    public string Status { get; set; } = string.Empty;
}

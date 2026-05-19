using System.ComponentModel.DataAnnotations;

namespace Sales.Web.Dtos.v1.Admin;

public class AdminOrderStatusDto
{
    [Required]
    public string Status { get; set; } = string.Empty;
}

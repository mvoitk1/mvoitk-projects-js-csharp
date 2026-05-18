using System.ComponentModel.DataAnnotations;

namespace Sales.Application.Dtos.Orders;

public class CreateOrderDto
{
    [Required] public string ShippingFirstName { get; set; } = string.Empty;
    [Required] public string ShippingLastName { get; set; } = string.Empty;
    [Required, EmailAddress] public string ShippingEmail { get; set; } = string.Empty;
    [Required] public string ShippingPhone { get; set; } = string.Empty;
    [Required] public string ShippingCountry { get; set; } = string.Empty;
    [Required] public string ShippingCity { get; set; } = string.Empty;
    [Required] public string ShippingStreet { get; set; } = string.Empty;
    [Required] public string ShippingPostalCode { get; set; } = string.Empty;
}

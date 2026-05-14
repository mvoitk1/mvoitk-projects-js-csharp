using App.Domain;
using App.DTO.v1.Admin;

namespace App.BLL.Mappers;

public static class AdminOrderMapper
{
    public static AdminOrderDto ToDto(Order e) => new()
    {
        Id = e.Id,
        OrderNumber = e.OrderNumber,
        Status = e.Status.ToString(),
        TotalAmount = e.TotalAmount,
        CreatedAt = MapperHelpers.ToLocal(e.CreatedAt),
        CustomerFirstName = e.AppUser?.FirstName ?? string.Empty,
        CustomerLastName = e.AppUser?.LastName ?? string.Empty,
        CustomerEmail = e.AppUser?.Email ?? string.Empty,
        ShippingFirstName = e.ShippingFirstName,
        ShippingLastName = e.ShippingLastName,
        ShippingEmail = e.ShippingEmail,
        ShippingPhone = e.ShippingPhone,
        ShippingCountry = e.ShippingCountry,
        ShippingCity = e.ShippingCity,
        ShippingStreet = e.ShippingStreet,
        ShippingPostalCode = e.ShippingPostalCode,
        Items = e.Items?.Select(OrderMapper.ToItemDto).ToList() ?? []
    };
}

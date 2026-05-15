using App.Domain;
using App.BLL.DTO.Orders;

namespace App.BLL.Mappers;

public static class OrderMapper
{
    public static OrderDto ToDto(Order e) => new()
    {
        Id = e.Id,
        OrderNumber = e.OrderNumber,
        Status = e.Status.ToString(),
        TotalAmount = e.TotalAmount,
        CreatedAt = MapperHelpers.ToLocal(e.CreatedAt),
        ShippingFirstName = e.ShippingFirstName,
        ShippingLastName = e.ShippingLastName,
        ShippingEmail = e.ShippingEmail,
        ShippingPhone = e.ShippingPhone,
        ShippingCountry = e.ShippingCountry,
        ShippingCity = e.ShippingCity,
        ShippingStreet = e.ShippingStreet,
        ShippingPostalCode = e.ShippingPostalCode,
        Items = e.Items?.Select(ToItemDto).ToList() ?? []
    };

    public static OrderListItemDto ToListItem(Order e) => new()
    {
        Id = e.Id,
        OrderNumber = e.OrderNumber,
        Status = e.Status.ToString(),
        TotalAmount = e.TotalAmount,
        CreatedAt = MapperHelpers.ToLocal(e.CreatedAt),
        ItemCount = e.Items?.Sum(i => i.Quantity) ?? 0
    };

    public static OrderItemDto ToItemDto(OrderItem i) => new()
    {
        Id = i.Id,
        Quantity = i.Quantity,
        UnitPrice = i.UnitPrice,
        LineTotal = i.LineTotal,
        ProductVariantId = i.ProductVariantId,
        ProductName = i.ProductVariant?.Product?.Name.Tr() ?? string.Empty,
        Sku = i.ProductVariant?.Sku ?? string.Empty,
        ColorName = i.ProductVariant?.Color?.Name.Tr() ?? string.Empty,
        SizeCode = i.ProductVariant?.Size?.SizeCode ?? string.Empty
    };
}

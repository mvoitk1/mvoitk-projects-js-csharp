using Modules.Contracts.Users.Queries;
using Sales.Application.Dtos.Admin;
using Sales.Application.Dtos.Orders;
using Sales.Domain;

namespace Sales.Application.Mappers;

public static class AdminOrderMapper
{
    private static readonly TimeZoneInfo Tz =
        TimeZoneInfo.FindSystemTimeZoneById("Europe/Tallinn");

    public static AdminOrderDto ToDto(Order e, UserSnapshotDto? customer) => new()
    {
        Id = e.Id,
        OrderNumber = e.OrderNumber,
        Status = e.Status.ToString(),
        TotalAmount = e.TotalAmount,
        CreatedAt = TimeZoneInfo.ConvertTimeFromUtc(e.CreatedAt, Tz),
        CustomerFirstName = customer?.FirstName ?? string.Empty,
        CustomerLastName = customer?.LastName ?? string.Empty,
        CustomerEmail = customer?.Email ?? string.Empty,
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

    private static OrderItemDto ToItemDto(OrderItem i) => new()
    {
        Id = i.Id,
        Quantity = i.Quantity,
        UnitPrice = i.UnitPrice,
        LineTotal = i.LineTotal,
        ProductVariantId = i.ProductVariantId,
        ProductName = i.ProductName,
        Sku = i.ProductVariantSku
    };
}

using App.Domain;
using App.BLL.DTO.Admin;

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
        // Customer name/email moved to the Users module — admin order list will be
        // hydrated via MediatR (GetUserSnapshotQuery) in Step 5 of the modular split.
        // Until then, the snapshot is left empty and the AppUserId surfaces in admin UX.
        CustomerFirstName = string.Empty,
        CustomerLastName = string.Empty,
        CustomerEmail = string.Empty,
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

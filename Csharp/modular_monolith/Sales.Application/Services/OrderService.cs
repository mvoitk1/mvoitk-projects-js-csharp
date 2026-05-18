using MediatR;
using Modules.Contracts.Catalog.Commands;
using Modules.Contracts.Catalog.Queries;
using Modules.Contracts.Sales.Events;
using Sales.Application.Contracts;
using Sales.Application.Dtos.Orders;
using Sales.Domain;
using Sales.Domain.Enums;

namespace Sales.Application.Services;

/// <summary>
/// Order use-cases. PlaceOrderAsync uses MediatR to reserve stock atomically in
/// the Catalog module before writing the order, and falls back to ReleaseStock
/// if the write fails. Customer name lookups on admin queries go through
/// GetUserSnapshotQuery (Users module).
/// </summary>
public class OrderService(ISalesUnitOfWork uow, IMediator mediator, IPublisher publisher) : IOrderService
{
    public async Task<OrderDto> PlaceOrderAsync(Guid userId, CreateOrderDto dto)
    {
        var cart = await uow.Carts.GetActiveCartForCheckoutAsync(userId);
        if (cart == null || cart.Items == null || !cart.Items.Any())
            throw new InvalidOperationException("No active cart with items found.");

        var lines = cart.Items
            .Select(i => new ReserveStockLine(i.ProductVariantId, i.Quantity))
            .ToList();

        var reservation = await mediator.Send(new ReserveStockCommand(lines));
        if (!reservation.Success)
            throw new InvalidOperationException(
                $"Insufficient stock for variants: {string.Join(", ", reservation.InsufficientStockVariantIds)}");

        var pricingMap = await mediator.Send(new GetVariantsPricingQuery(
            cart.Items.Select(i => i.ProductVariantId).Distinct().ToList()));

        try
        {
            var totalAmount = cart.Items.Sum(i => i.UnitPrice * i.Quantity);
            var order = new Order
            {
                OrderNumber = GenerateOrderNumber(),
                Status = OrderStatus.Confirmed,
                TotalAmount = totalAmount,
                AppUserId = userId,
                ShippingFirstName = dto.ShippingFirstName,
                ShippingLastName = dto.ShippingLastName,
                ShippingEmail = dto.ShippingEmail,
                ShippingPhone = dto.ShippingPhone,
                ShippingCountry = dto.ShippingCountry,
                ShippingCity = dto.ShippingCity,
                ShippingStreet = dto.ShippingStreet,
                ShippingPostalCode = dto.ShippingPostalCode,
                Items = cart.Items.Select(i =>
                {
                    pricingMap.TryGetValue(i.ProductVariantId, out var p);
                    return new OrderItem
                    {
                        ProductVariantId = i.ProductVariantId,
                        Quantity = i.Quantity,
                        UnitPrice = i.UnitPrice,
                        LineTotal = i.UnitPrice * i.Quantity,
                        ProductVariantSku = p?.Sku ?? string.Empty,
                        ProductName = p?.ProductName ?? string.Empty
                    };
                }).ToList()
            };

            uow.CartItems.RemoveRange(cart.Items);
            cart.Status = CartStatus.CheckedOut;
            cart.UpdatedAt = DateTime.UtcNow;

            uow.Orders.Add(order);
            await uow.SaveChangesAsync();

            await publisher.Publish(new OrderPlacedEvent(
                order.Id,
                order.OrderNumber,
                userId,
                totalAmount,
                order.Items.Select(i => new OrderPlacedLine(i.ProductVariantId, i.Quantity, i.UnitPrice)).ToList()));

            return (await GetUserOrderByIdAsync(userId, order.Id))!;
        }
        catch
        {
            // Compensating: release the stock we just reserved.
            await mediator.Send(new ReleaseStockCommand(lines));
            throw;
        }
    }

    public async Task<IEnumerable<OrderListItemDto>> GetUserOrdersAsync(Guid userId)
    {
        var orders = await uow.Orders.GetUserOrdersAsync(userId);
        return orders.Select(ToListItem);
    }

    public async Task<OrderDto?> GetUserOrderByIdAsync(Guid userId, Guid orderId)
    {
        var order = await uow.Orders.GetUserOrderByIdAsync(userId, orderId);
        return order == null ? null : ToDto(order);
    }

    private static string GenerateOrderNumber()
    {
        return $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
    }

    private static OrderListItemDto ToListItem(Order o) => new()
    {
        Id = o.Id,
        OrderNumber = o.OrderNumber,
        Status = o.Status.ToString(),
        TotalAmount = o.TotalAmount,
        CreatedAt = o.CreatedAt,
        ItemCount = o.Items?.Sum(i => i.Quantity) ?? 0
    };

    private static OrderDto ToDto(Order o) => new()
    {
        Id = o.Id,
        OrderNumber = o.OrderNumber,
        Status = o.Status.ToString(),
        TotalAmount = o.TotalAmount,
        CreatedAt = o.CreatedAt,
        ShippingFirstName = o.ShippingFirstName,
        ShippingLastName = o.ShippingLastName,
        ShippingEmail = o.ShippingEmail,
        ShippingPhone = o.ShippingPhone,
        ShippingCountry = o.ShippingCountry,
        ShippingCity = o.ShippingCity,
        ShippingStreet = o.ShippingStreet,
        ShippingPostalCode = o.ShippingPostalCode,
        Items = o.Items?.Select(i => new OrderItemDto
        {
            Id = i.Id,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice,
            LineTotal = i.LineTotal,
            ProductVariantId = i.ProductVariantId,
            ProductName = i.ProductName,
            Sku = i.ProductVariantSku
        }).ToList() ?? new List<OrderItemDto>()
    };
}

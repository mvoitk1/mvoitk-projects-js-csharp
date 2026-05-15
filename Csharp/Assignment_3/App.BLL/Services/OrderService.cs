using App.BLL.Contracts;
using App.BLL.Mappers;
using App.DAL.Contracts.UnitOfWork;
using App.Domain;
using App.Domain.Enums;
using App.BLL.DTO.Orders;

namespace App.BLL.Services;

public class OrderService(IAppUnitOfWork uow) : IOrderService
{
    public async Task<OrderDto> PlaceOrderAsync(Guid userId, CreateOrderDto dto)
    {
        var cart = await uow.Carts.GetActiveCartForCheckoutAsync(userId);

        if (cart == null || cart.Items == null || !cart.Items.Any())
            throw new InvalidOperationException("No active cart with items found.");

        // Verify stock for all items
        foreach (var item in cart.Items)
        {
            if (item.ProductVariant == null || item.ProductVariant.StockQty < item.Quantity)
                throw new InvalidOperationException($"Insufficient stock for SKU {item.ProductVariant?.Sku}.");
        }

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
            Items = cart.Items.Select(i => new OrderItem
            {
                ProductVariantId = i.ProductVariantId,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                LineTotal = i.UnitPrice * i.Quantity
            }).ToList()
        };

        // Deduct stock
        foreach (var item in cart.Items)
        {
            item.ProductVariant!.StockQty -= item.Quantity;
        }

        // Clear cart items and mark as checked out
        uow.CartItems.RemoveRange(cart.Items);
        cart.Status = CartStatus.CheckedOut;
        cart.UpdatedAt = DateTime.UtcNow;

        uow.Orders.Add(order);
        await uow.SaveChangesAsync();

        return (await GetUserOrderByIdAsync(userId, order.Id))!;
    }

    public async Task<IEnumerable<OrderListItemDto>> GetUserOrdersAsync(Guid userId)
    {
        var orders = await uow.Orders.GetUserOrdersAsync(userId);
        return orders.Select(OrderMapper.ToListItem);
    }

    public async Task<OrderDto?> GetUserOrderByIdAsync(Guid userId, Guid orderId)
    {
        var order = await uow.Orders.GetUserOrderByIdAsync(userId, orderId);
        return order == null ? null : OrderMapper.ToDto(order);
    }

    private static string GenerateOrderNumber()
    {
        return $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
    }
}

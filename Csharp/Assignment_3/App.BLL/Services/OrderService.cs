using App.DAL.EF;
using App.Domain;
using App.Domain.Enums;
using App.DTO.v1.Orders;
using Microsoft.EntityFrameworkCore;

namespace App.BLL.Services;

public class OrderService(AppDbContext db) : IOrderService
{
    public async Task<OrderDto> PlaceOrderAsync(Guid userId, CreateOrderDto dto)
    {
        var cart = await db.Carts
            .Include(c => c.Items)!
                .ThenInclude(i => i.ProductVariant)
            .FirstOrDefaultAsync(c => c.AppUserId == userId && c.Status == CartStatus.Active);

        if (cart == null || cart.Items == null || !cart.Items.Any())
            throw new InvalidOperationException("No active cart with items found.");

        // Verify stock for all items
        foreach (var item in cart.Items)
        {
            if (item.ProductVariant == null || item.ProductVariant.StockQty < item.Quantity)
                throw new InvalidOperationException($"Insufficient stock for SKU {item.ProductVariant?.Sku}.");
        }

        var totalAmount = cart.Items.Sum(i => i.UnitPrice * i.Quantity);
        var orderNumber = GenerateOrderNumber();

        var order = new Order
        {
            OrderNumber = orderNumber,
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

        // Mark cart as checked out
        cart.Status = CartStatus.CheckedOut;
        cart.UpdatedAt = DateTime.UtcNow;

        db.Orders.Add(order);
        await db.SaveChangesAsync();

        return (await GetUserOrderByIdAsync(userId, order.Id))!;
    }

    public async Task<IEnumerable<OrderListItemDto>> GetUserOrdersAsync(Guid userId)
    {
        var orders = await db.Orders
            .Include(o => o.Items)
            .Where(o => o.AppUserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return orders.Select(o => new OrderListItemDto
        {
            Id = o.Id,
            OrderNumber = o.OrderNumber,
            Status = o.Status.ToString(),
            TotalAmount = o.TotalAmount,
            CreatedAt = o.CreatedAt,
            ItemCount = o.Items?.Sum(i => i.Quantity) ?? 0
        });
    }

    public async Task<OrderDto?> GetUserOrderByIdAsync(Guid userId, Guid orderId)
    {
        var order = await db.Orders
            .Include(o => o.Items)!
                .ThenInclude(i => i.ProductVariant)
                    .ThenInclude(v => v!.Color)
            .Include(o => o.Items)!
                .ThenInclude(i => i.ProductVariant)
                    .ThenInclude(v => v!.Size)
            .Include(o => o.Items)!
                .ThenInclude(i => i.ProductVariant)
                    .ThenInclude(v => v!.Product)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.AppUserId == userId);

        if (order == null) return null;

        return new OrderDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            Status = order.Status.ToString(),
            TotalAmount = order.TotalAmount,
            CreatedAt = order.CreatedAt,
            ShippingFirstName = order.ShippingFirstName,
            ShippingLastName = order.ShippingLastName,
            ShippingEmail = order.ShippingEmail,
            ShippingPhone = order.ShippingPhone,
            ShippingCountry = order.ShippingCountry,
            ShippingCity = order.ShippingCity,
            ShippingStreet = order.ShippingStreet,
            ShippingPostalCode = order.ShippingPostalCode,
            Items = order.Items?.Select(i => new OrderItemDto
            {
                Id = i.Id,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                LineTotal = i.LineTotal,
                ProductVariantId = i.ProductVariantId,
                ProductName = i.ProductVariant?.Product?.Name.Translate() ?? string.Empty,
                Sku = i.ProductVariant?.Sku ?? string.Empty,
                ColorName = i.ProductVariant?.Color?.Name.Translate() ?? string.Empty,
                SizeCode = i.ProductVariant?.Size?.SizeCode ?? string.Empty
            }).ToList() ?? []
        };
    }

    private static string GenerateOrderNumber()
    {
        return $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
    }
}

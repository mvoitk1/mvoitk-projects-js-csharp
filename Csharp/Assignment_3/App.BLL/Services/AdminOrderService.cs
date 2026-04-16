using App.DAL.EF;
using App.Domain.Enums;
using App.DTO.v1.Admin;
using App.DTO.v1.Orders;
using Microsoft.EntityFrameworkCore;

namespace App.BLL.Services;

public class AdminOrderService(AppDbContext db) : IAdminOrderService
{
    private static readonly TimeZoneInfo _tz =
        TimeZoneInfo.FindSystemTimeZoneById("Europe/Tallinn");

    private static DateTime ToLocal(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(utc, _tz);

    public async Task<IEnumerable<AdminOrderDto>> GetAllAsync(string? statusFilter = null)
    {
        var query = db.Orders
            .Include(o => o.AppUser)
            .Include(o => o.Items)!
                .ThenInclude(i => i.ProductVariant)
                    .ThenInclude(v => v!.Color)
            .Include(o => o.Items)!
                .ThenInclude(i => i.ProductVariant)
                    .ThenInclude(v => v!.Size)
            .Include(o => o.Items)!
                .ThenInclude(i => i.ProductVariant)
                    .ThenInclude(v => v!.Product)
            .AsQueryable();

        if (!string.IsNullOrEmpty(statusFilter) &&
            Enum.TryParse<OrderStatus>(statusFilter, true, out var status))
        {
            query = query.Where(o => o.Status == status);
        }

        var orders = await query.OrderByDescending(o => o.CreatedAt).ToListAsync();

        return orders.Select(o => new AdminOrderDto
        {
            Id = o.Id,
            OrderNumber = o.OrderNumber,
            Status = o.Status.ToString(),
            TotalAmount = o.TotalAmount,
            CreatedAt = ToLocal(o.CreatedAt),
            CustomerFirstName = o.AppUser?.FirstName ?? string.Empty,
            CustomerLastName = o.AppUser?.LastName ?? string.Empty,
            CustomerEmail = o.AppUser?.Email ?? string.Empty,
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
                ProductName = i.ProductVariant?.Product?.Name.Translate() ?? string.Empty,
                Sku = i.ProductVariant?.Sku ?? string.Empty,
                ColorName = i.ProductVariant?.Color?.Name.Translate() ?? string.Empty,
                SizeCode = i.ProductVariant?.Size?.SizeCode ?? string.Empty
            }).ToList() ?? []
        });
    }

    public async Task<AdminOrderDto?> GetByIdAsync(Guid id)
    {
        var orders = await GetAllAsync();
        return orders.FirstOrDefault(o => o.Id == id);
    }

    public async Task<bool> UpdateStatusAsync(Guid id, string status)
    {
        if (!Enum.TryParse<OrderStatus>(status, true, out var orderStatus))
            return false;

        var order = await db.Orders.FindAsync(id);
        if (order == null) return false;

        order.Status = orderStatus;
        await db.SaveChangesAsync();
        return true;
    }
}

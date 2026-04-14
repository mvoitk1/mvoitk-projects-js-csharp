using App.DAL.EF;
using App.Domain;
using App.Domain.Enums;
using App.DTO.v1.Cart;
using Microsoft.EntityFrameworkCore;

namespace App.BLL.Services;

public class CartService(AppDbContext db) : ICartService
{
    public async Task<CartDto> GetOrCreateCartAsync(Guid userId)
    {
        var cart = await GetActiveCartWithItemsAsync(userId);

        if (cart == null)
        {
            cart = new Cart { AppUserId = userId };
            db.Carts.Add(cart);
            await db.SaveChangesAsync();
            cart = await GetActiveCartWithItemsAsync(userId);
        }

        return MapToDto(cart!);
    }

    public async Task<CartDto> AddItemAsync(Guid userId, AddToCartDto dto)
    {
        var cart = await GetActiveCartWithItemsAsync(userId);
        if (cart == null)
        {
            cart = new Cart { AppUserId = userId };
            db.Carts.Add(cart);
            await db.SaveChangesAsync();
            cart = await GetActiveCartWithItemsAsync(userId);
        }

        var variant = await db.ProductVariants
            .FirstOrDefaultAsync(v => v.Id == dto.ProductVariantId && v.IsActive);

        if (variant == null)
            throw new InvalidOperationException("Product variant not found.");

        if (variant.StockQty < dto.Quantity)
            throw new InvalidOperationException("Insufficient stock.");

        var existingItem = cart!.Items?.FirstOrDefault(i => i.ProductVariantId == dto.ProductVariantId);
        if (existingItem != null)
        {
            existingItem.Quantity += dto.Quantity;
        }
        else
        {
            db.CartItems.Add(new CartItem
            {
                CartId = cart.Id,
                ProductVariantId = dto.ProductVariantId,
                Quantity = dto.Quantity,
                UnitPrice = variant.Price
            });
        }

        cart.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return MapToDto((await GetActiveCartWithItemsAsync(userId))!);
    }

    public async Task<CartDto> UpdateItemAsync(Guid userId, Guid cartItemId, UpdateCartItemDto dto)
    {
        var cart = await GetActiveCartWithItemsAsync(userId);
        var item = cart?.Items?.FirstOrDefault(i => i.Id == cartItemId);

        if (item == null)
            throw new InvalidOperationException("Cart item not found.");

        var variant = await db.ProductVariants.FindAsync(item.ProductVariantId);
        if (variant == null || variant.StockQty < dto.Quantity)
            throw new InvalidOperationException("Insufficient stock.");

        item.Quantity = dto.Quantity;
        cart!.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return MapToDto((await GetActiveCartWithItemsAsync(userId))!);
    }

    public async Task RemoveItemAsync(Guid userId, Guid cartItemId)
    {
        var cart = await GetActiveCartWithItemsAsync(userId);
        var item = cart?.Items?.FirstOrDefault(i => i.Id == cartItemId);

        if (item == null)
            throw new InvalidOperationException("Cart item not found.");

        db.CartItems.Remove(item);
        cart!.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    private async Task<Cart?> GetActiveCartWithItemsAsync(Guid userId)
    {
        return await db.Carts
            .Include(c => c.Items)!
                .ThenInclude(i => i.ProductVariant)
                    .ThenInclude(v => v!.Color)
            .Include(c => c.Items)!
                .ThenInclude(i => i.ProductVariant)
                    .ThenInclude(v => v!.Size)
            .Include(c => c.Items)!
                .ThenInclude(i => i.ProductVariant)
                    .ThenInclude(v => v!.Product)
                        .ThenInclude(p => p!.Images)
            .FirstOrDefaultAsync(c => c.AppUserId == userId && c.Status == CartStatus.Active);
    }

    private static CartDto MapToDto(Cart cart) => new CartDto
    {
        Id = cart.Id,
        Status = cart.Status.ToString(),
        Items = cart.Items?.Select(i => new CartItemDto
        {
            Id = i.Id,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice,
            ProductVariantId = i.ProductVariantId,
            ProductName = i.ProductVariant?.Product?.Name.Translate() ?? string.Empty,
            Sku = i.ProductVariant?.Sku ?? string.Empty,
            ColorName = i.ProductVariant?.Color?.Name.Translate() ?? string.Empty,
            SizeCode = i.ProductVariant?.Size?.SizeCode ?? string.Empty,
            ImageUrl = i.ProductVariant?.Product?.Images?
                .OrderBy(img => img.SortOrder).FirstOrDefault()?.Url
        }).ToList() ?? []
    };
}

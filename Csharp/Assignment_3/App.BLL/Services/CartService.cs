using App.BLL.Contracts;
using App.BLL.Mappers;
using App.DAL.Contracts.UnitOfWork;
using App.Domain;
using App.BLL.DTO.Cart;

namespace App.BLL.Services;

public class CartService(IAppUnitOfWork uow) : ICartService
{
    public async Task<CartDto> GetOrCreateCartAsync(Guid userId)
    {
        var cart = await uow.Carts.GetActiveCartForUserAsync(userId);

        if (cart == null)
        {
            uow.Carts.Add(new Cart { AppUserId = userId });
            await uow.SaveChangesAsync();
            cart = await uow.Carts.GetActiveCartForUserAsync(userId);
        }

        return CartMapper.ToDto(cart!);
    }

    public async Task<CartDto> AddItemAsync(Guid userId, AddToCartDto dto)
    {
        var cart = await uow.Carts.GetActiveCartForUserAsync(userId);
        if (cart == null)
        {
            uow.Carts.Add(new Cart { AppUserId = userId });
            await uow.SaveChangesAsync();
            cart = await uow.Carts.GetActiveCartForUserAsync(userId);
        }

        var variant = await uow.ProductVariants.FindActiveAsync(dto.ProductVariantId);
        if (variant == null)
            throw new InvalidOperationException("Product variant not found.");

        if (variant.StockQty < dto.Quantity)
            throw new InvalidOperationException("Insufficient stock.");

        var existingItem = cart!.Items?.FirstOrDefault(i => i.ProductVariantId == dto.ProductVariantId);
        if (existingItem != null)
        {
            existingItem.Quantity += dto.Quantity;
            uow.CartItems.Update(existingItem);
        }
        else
        {
            uow.CartItems.Add(new CartItem
            {
                CartId = cart.Id,
                ProductVariantId = dto.ProductVariantId,
                Quantity = dto.Quantity,
                UnitPrice = variant.Price
            });
        }

        cart.UpdatedAt = DateTime.UtcNow;
        uow.Carts.Update(cart);
        await uow.SaveChangesAsync();

        return CartMapper.ToDto((await uow.Carts.GetActiveCartForUserAsync(userId))!);
    }

    public async Task<CartDto> UpdateItemAsync(Guid userId, Guid cartItemId, UpdateCartItemDto dto)
    {
        var cart = await uow.Carts.GetActiveCartForUserAsync(userId);
        var item = cart?.Items?.FirstOrDefault(i => i.Id == cartItemId);

        if (item == null)
            throw new InvalidOperationException("Cart item not found.");

        var variant = await uow.ProductVariants.FindAsync(item.ProductVariantId);
        if (variant == null || variant.StockQty < dto.Quantity)
            throw new InvalidOperationException("Insufficient stock.");

        item.Quantity = dto.Quantity;
        uow.CartItems.Update(item);
        cart!.UpdatedAt = DateTime.UtcNow;
        uow.Carts.Update(cart);
        await uow.SaveChangesAsync();

        return CartMapper.ToDto((await uow.Carts.GetActiveCartForUserAsync(userId))!);
    }

    public async Task RemoveItemAsync(Guid userId, Guid cartItemId)
    {
        var cart = await uow.Carts.GetActiveCartForUserAsync(userId);
        var item = cart?.Items?.FirstOrDefault(i => i.Id == cartItemId);

        if (item == null)
            throw new InvalidOperationException("Cart item not found.");

        uow.CartItems.Remove(item);
        cart!.UpdatedAt = DateTime.UtcNow;
        uow.Carts.Update(cart);
        await uow.SaveChangesAsync();
    }
}

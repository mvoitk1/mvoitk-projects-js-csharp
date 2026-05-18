using MediatR;
using Modules.Contracts.Catalog.Queries;
using Sales.Application.Contracts;
using Sales.Application.Dtos.Cart;
using Sales.Domain;

namespace Sales.Application.Services;

/// <summary>
/// Cart use-cases. Every read of catalog data (price, stock, product name, image)
/// goes through MediatR — Sales has no direct reference to Catalog.
/// </summary>
public class CartService(ISalesUnitOfWork uow, IMediator mediator) : ICartService
{
    public async Task<CartDto> GetOrCreateCartAsync(Guid userId)
    {
        var cart = await uow.Carts.GetActiveCartForUserAsync(userId)
                   ?? await CreateEmptyCartAsync(userId);
        return await BuildDtoAsync(cart);
    }

    public async Task<CartDto> AddItemAsync(Guid userId, AddToCartDto dto)
    {
        var cart = await uow.Carts.GetActiveCartForUserAsync(userId)
                   ?? await CreateEmptyCartAsync(userId);

        var pricing = await mediator.Send(new GetVariantPricingQuery(dto.ProductVariantId));
        if (pricing is null || !pricing.IsActive)
            throw new InvalidOperationException("Product variant not found.");
        if (pricing.AvailableQty < dto.Quantity)
            throw new InvalidOperationException("Insufficient stock.");

        var existing = cart.Items?.FirstOrDefault(i => i.ProductVariantId == dto.ProductVariantId);
        if (existing != null)
        {
            existing.Quantity += dto.Quantity;
            uow.CartItems.Update(existing);
        }
        else
        {
            uow.CartItems.Add(new CartItem
            {
                CartId = cart.Id,
                ProductVariantId = dto.ProductVariantId,
                Quantity = dto.Quantity,
                UnitPrice = pricing.Price
            });
        }

        cart.UpdatedAt = DateTime.UtcNow;
        uow.Carts.Update(cart);
        await uow.SaveChangesAsync();

        var refreshed = await uow.Carts.GetActiveCartForUserAsync(userId);
        return await BuildDtoAsync(refreshed!);
    }

    public async Task<CartDto> UpdateItemAsync(Guid userId, Guid cartItemId, UpdateCartItemDto dto)
    {
        var cart = await uow.Carts.GetActiveCartForUserAsync(userId);
        var item = cart?.Items?.FirstOrDefault(i => i.Id == cartItemId);
        if (item == null) throw new InvalidOperationException("Cart item not found.");

        var pricing = await mediator.Send(new GetVariantPricingQuery(item.ProductVariantId));
        if (pricing is null || pricing.AvailableQty < dto.Quantity)
            throw new InvalidOperationException("Insufficient stock.");

        item.Quantity = dto.Quantity;
        uow.CartItems.Update(item);
        cart!.UpdatedAt = DateTime.UtcNow;
        uow.Carts.Update(cart);
        await uow.SaveChangesAsync();

        var refreshed = await uow.Carts.GetActiveCartForUserAsync(userId);
        return await BuildDtoAsync(refreshed!);
    }

    public async Task RemoveItemAsync(Guid userId, Guid cartItemId)
    {
        var cart = await uow.Carts.GetActiveCartForUserAsync(userId);
        var item = cart?.Items?.FirstOrDefault(i => i.Id == cartItemId);
        if (item == null) throw new InvalidOperationException("Cart item not found.");

        uow.CartItems.Remove(item);
        cart!.UpdatedAt = DateTime.UtcNow;
        uow.Carts.Update(cart);
        await uow.SaveChangesAsync();
    }

    private async Task<Cart> CreateEmptyCartAsync(Guid userId)
    {
        uow.Carts.Add(new Cart { AppUserId = userId });
        await uow.SaveChangesAsync();
        return (await uow.Carts.GetActiveCartForUserAsync(userId))!;
    }

    private async Task<CartDto> BuildDtoAsync(Cart cart)
    {
        var items = cart.Items ?? new List<CartItem>();
        var variantIds = items.Select(i => i.ProductVariantId).Distinct().ToList();
        var pricingMap = variantIds.Count == 0
            ? new Dictionary<Guid, VariantPricingDto>()
            : (await mediator.Send(new GetVariantsPricingQuery(variantIds)))
                .ToDictionary(kv => kv.Key, kv => kv.Value);

        return new CartDto
        {
            Id = cart.Id,
            Status = cart.Status.ToString(),
            Items = items.Select(i =>
            {
                pricingMap.TryGetValue(i.ProductVariantId, out var p);
                return new CartItemDto
                {
                    Id = i.Id,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    ProductVariantId = i.ProductVariantId,
                    ProductName = p?.ProductName ?? string.Empty,
                    Sku = p?.Sku ?? string.Empty,
                    ColorName = p?.ColorName ?? string.Empty,
                    SizeCode = p?.SizeCode ?? string.Empty,
                    ImageUrl = p?.FirstImageUrl
                };
            }).ToList()
        };
    }
}

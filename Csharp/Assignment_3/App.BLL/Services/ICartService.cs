using App.DTO.v1.Cart;

namespace App.BLL.Services;

public interface ICartService
{
    Task<CartDto> GetOrCreateCartAsync(Guid userId);
    Task<CartDto> AddItemAsync(Guid userId, AddToCartDto dto);
    Task<CartDto> UpdateItemAsync(Guid userId, Guid cartItemId, UpdateCartItemDto dto);
    Task RemoveItemAsync(Guid userId, Guid cartItemId);
}

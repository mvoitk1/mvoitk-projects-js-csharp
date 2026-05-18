using App.BLL.DTO.Cart;

namespace App.BLL.Contracts;

public interface ICartService
{
    Task<CartDto> GetOrCreateCartAsync(Guid userId);
    Task<CartDto> AddItemAsync(Guid userId, AddToCartDto dto);
    Task<CartDto> UpdateItemAsync(Guid userId, Guid cartItemId, UpdateCartItemDto dto);
    Task RemoveItemAsync(Guid userId, Guid cartItemId);
}

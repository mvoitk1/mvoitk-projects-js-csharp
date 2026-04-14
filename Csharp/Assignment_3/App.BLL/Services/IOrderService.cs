using App.DTO.v1.Orders;

namespace App.BLL.Services;

public interface IOrderService
{
    Task<OrderDto> PlaceOrderAsync(Guid userId, CreateOrderDto dto);
    Task<IEnumerable<OrderListItemDto>> GetUserOrdersAsync(Guid userId);
    Task<OrderDto?> GetUserOrderByIdAsync(Guid userId, Guid orderId);
}

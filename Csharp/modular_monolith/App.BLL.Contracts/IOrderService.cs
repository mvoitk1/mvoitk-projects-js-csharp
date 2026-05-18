using App.BLL.DTO.Orders;

namespace App.BLL.Contracts;

public interface IOrderService
{
    Task<OrderDto> PlaceOrderAsync(Guid userId, CreateOrderDto dto);
    Task<IEnumerable<OrderListItemDto>> GetUserOrdersAsync(Guid userId);
    Task<OrderDto?> GetUserOrderByIdAsync(Guid userId, Guid orderId);
}

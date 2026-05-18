using Sales.Application.Dtos.Orders;

namespace Sales.Application.Contracts;

public interface IOrderService
{
    Task<OrderDto> PlaceOrderAsync(Guid userId, CreateOrderDto dto);
    Task<IEnumerable<OrderListItemDto>> GetUserOrdersAsync(Guid userId);
    Task<OrderDto?> GetUserOrderByIdAsync(Guid userId, Guid orderId);
}

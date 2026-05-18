using Base.DAL.Contracts;
using Sales.Domain;
using Sales.Domain.Enums;

namespace Sales.Application.Contracts.Repositories;

public interface IOrderRepository : IBaseRepository<Order>
{
    Task<IEnumerable<Order>> GetUserOrdersAsync(Guid userId);
    Task<Order?> GetUserOrderByIdAsync(Guid userId, Guid orderId);
    Task<IEnumerable<Order>> GetAllWithDetailsAsync(OrderStatus? status = null);
    Task<Order?> GetByIdWithDetailsAsync(Guid id);
}

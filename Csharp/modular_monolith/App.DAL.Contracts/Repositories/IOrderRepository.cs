using Base.DAL.Contracts;
using App.Domain;
using App.Domain.Enums;

namespace App.DAL.Contracts.Repositories;

public interface IOrderRepository : IBaseRepository<Order>
{
    /// <summary>All orders belonging to the given user, newest first (with items for counts).</summary>
    Task<IEnumerable<Order>> GetUserOrdersAsync(Guid userId);

    /// <summary>
    /// A single order with the full include graph — scoped to the owning user.
    /// The userId filter is enforced here so callers cannot read another user's order (IDOR).
    /// </summary>
    Task<Order?> GetUserOrderByIdAsync(Guid userId, Guid orderId);

    /// <summary>All orders with customer + item details, optionally filtered by status (admin).</summary>
    Task<IEnumerable<Order>> GetAllWithDetailsAsync(OrderStatus? status = null);

    /// <summary>A single order with customer + item details, not user-scoped (admin).</summary>
    Task<Order?> GetByIdWithDetailsAsync(Guid id);
}

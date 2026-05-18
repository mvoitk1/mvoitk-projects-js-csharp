using MediatR;
using Modules.Contracts.Users.Queries;
using Sales.Application.Contracts;
using Sales.Application.Dtos.Admin;
using Sales.Application.Mappers;
using Sales.Domain.Enums;

namespace Sales.Application.Services;

public class AdminOrderService(ISalesUnitOfWork uow, IMediator mediator) : IAdminOrderService
{
    public async Task<IEnumerable<AdminOrderDto>> GetAllAsync(string? statusFilter = null)
    {
        OrderStatus? status = null;
        if (!string.IsNullOrEmpty(statusFilter) &&
            Enum.TryParse<OrderStatus>(statusFilter, true, out var parsed))
            status = parsed;

        var orders = (await uow.Orders.GetAllWithDetailsAsync(status)).ToList();

        var snapshots = await LoadSnapshotsAsync(orders.Select(o => o.AppUserId));

        return orders.Select(o =>
        {
            snapshots.TryGetValue(o.AppUserId, out var snap);
            return AdminOrderMapper.ToDto(o, snap);
        }).ToList();
    }

    public async Task<AdminOrderDto?> GetByIdAsync(Guid id)
    {
        var order = await uow.Orders.GetByIdWithDetailsAsync(id);
        if (order == null) return null;

        var snap = await mediator.Send(new GetUserSnapshotQuery(order.AppUserId));
        return AdminOrderMapper.ToDto(order, snap);
    }

    public async Task<bool> UpdateStatusAsync(Guid id, string status)
    {
        if (!Enum.TryParse<OrderStatus>(status, true, out var orderStatus))
            return false;

        var order = await uow.Orders.FindAsync(id);
        if (order == null) return false;

        order.Status = orderStatus;
        uow.Orders.Update(order);
        await uow.SaveChangesAsync();
        return true;
    }

    private async Task<Dictionary<Guid, UserSnapshotDto>> LoadSnapshotsAsync(IEnumerable<Guid> userIds)
    {
        var unique = userIds.Distinct().ToList();
        var result = new Dictionary<Guid, UserSnapshotDto>(unique.Count);
        foreach (var id in unique)
        {
            var snap = await mediator.Send(new GetUserSnapshotQuery(id));
            if (snap != null) result[id] = snap;
        }
        return result;
    }
}

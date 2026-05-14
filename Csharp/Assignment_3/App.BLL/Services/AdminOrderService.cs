using App.BLL.Mappers;
using App.DAL.EF.UnitOfWork;
using App.Domain.Enums;
using App.DTO.v1.Admin;

namespace App.BLL.Services;

public class AdminOrderService(IAppUnitOfWork uow) : IAdminOrderService
{
    public async Task<IEnumerable<AdminOrderDto>> GetAllAsync(string? statusFilter = null)
    {
        OrderStatus? status = null;
        if (!string.IsNullOrEmpty(statusFilter) &&
            Enum.TryParse<OrderStatus>(statusFilter, true, out var parsed))
            status = parsed;

        var orders = await uow.Orders.GetAllWithDetailsAsync(status);
        return orders.Select(AdminOrderMapper.ToDto);
    }

    public async Task<AdminOrderDto?> GetByIdAsync(Guid id)
    {
        var order = await uow.Orders.GetByIdWithDetailsAsync(id);
        return order == null ? null : AdminOrderMapper.ToDto(order);
    }

    public async Task<bool> UpdateStatusAsync(Guid id, string status)
    {
        if (!Enum.TryParse<OrderStatus>(status, true, out var orderStatus))
            return false;

        var order = await uow.Orders.FindAsync(id);
        if (order == null) return false;

        order.Status = orderStatus;
        await uow.SaveChangesAsync();
        return true;
    }
}

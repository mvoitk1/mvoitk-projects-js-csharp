using App.BLL.DTO.Admin;

namespace App.BLL.Contracts;

public interface IAdminOrderService
{
    Task<IEnumerable<AdminOrderDto>> GetAllAsync(string? statusFilter = null);
    Task<AdminOrderDto?> GetByIdAsync(Guid id);
    Task<bool> UpdateStatusAsync(Guid id, string status);
}

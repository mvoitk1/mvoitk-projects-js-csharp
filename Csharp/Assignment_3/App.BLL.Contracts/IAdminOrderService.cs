using App.DTO.v1.Admin;

namespace App.BLL.Services;

public interface IAdminOrderService
{
    Task<IEnumerable<AdminOrderDto>> GetAllAsync(string? statusFilter = null);
    Task<AdminOrderDto?> GetByIdAsync(Guid id);
    Task<bool> UpdateStatusAsync(Guid id, string status);
}

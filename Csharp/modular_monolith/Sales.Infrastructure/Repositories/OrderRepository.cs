using Base.DAL.EF;
using Microsoft.EntityFrameworkCore;
using Sales.Application.Contracts.Repositories;
using Sales.Domain;
using Sales.Domain.Enums;

namespace Sales.Infrastructure.Repositories;

public class OrderRepository(SalesDbContext dbContext)
    : BaseRepository<Order, SalesDbContext>(dbContext), IOrderRepository
{
    public async Task<IEnumerable<Order>> GetUserOrdersAsync(Guid userId)
    {
        return await RepoDbSet
            .Include(o => o.Items)
            .Where(o => o.AppUserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    public async Task<Order?> GetUserOrderByIdAsync(Guid userId, Guid orderId)
    {
        return await RepoDbSet
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.AppUserId == userId);
    }

    public async Task<IEnumerable<Order>> GetAllWithDetailsAsync(OrderStatus? status = null)
    {
        var query = RepoDbSet.Include(o => o.Items).AsQueryable();
        if (status.HasValue) query = query.Where(o => o.Status == status.Value);
        return await query.OrderByDescending(o => o.CreatedAt).ToListAsync();
    }

    public async Task<Order?> GetByIdWithDetailsAsync(Guid id)
    {
        return await RepoDbSet
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);
    }
}

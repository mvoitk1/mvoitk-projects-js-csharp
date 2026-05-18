using App.DAL.Contracts.Repositories;
using Base.DAL.EF;
using App.Domain;
using App.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.EF.Repositories;

public class OrderRepository : BaseRepository<Order, AppDbContext>, IOrderRepository
{
    public OrderRepository(AppDbContext dbContext) : base(dbContext)
    {
    }

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
        return await OrderWithDetails()
            .FirstOrDefaultAsync(o => o.Id == orderId && o.AppUserId == userId);
    }

    public async Task<IEnumerable<Order>> GetAllWithDetailsAsync(OrderStatus? status = null)
    {
        var query = OrderWithDetails()
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(o => o.Status == status.Value);

        return await query.OrderByDescending(o => o.CreatedAt).ToListAsync();
    }

    public async Task<Order?> GetByIdWithDetailsAsync(Guid id)
    {
        return await OrderWithDetails()
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    private IQueryable<Order> OrderWithDetails()
    {
        return RepoDbSet
            .Include(o => o.Items)!
                .ThenInclude(i => i.ProductVariant)
                    .ThenInclude(v => v!.Color)
            .Include(o => o.Items)!
                .ThenInclude(i => i.ProductVariant)
                    .ThenInclude(v => v!.Size)
            .Include(o => o.Items)!
                .ThenInclude(i => i.ProductVariant)
                    .ThenInclude(v => v!.Product);
    }
}

using App.DAL.Contracts.Repositories;
using Base.DAL.EF;
using App.Domain;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.EF.Repositories;

public class ProductImageRepository : BaseRepository<ProductImage, AppDbContext>, IProductImageRepository
{
    public ProductImageRepository(AppDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<ProductImage?> FindForProductAsync(Guid productId, Guid imageId)
    {
        return await RepoDbSet
            .FirstOrDefaultAsync(i => i.Id == imageId && i.ProductId == productId);
    }
}

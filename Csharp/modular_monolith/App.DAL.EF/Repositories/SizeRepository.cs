using App.DAL.Contracts.Repositories;
using Base.DAL.EF;
using App.Domain;

namespace App.DAL.EF.Repositories;

public class SizeRepository : BaseRepository<Size, AppDbContext>, ISizeRepository
{
    public SizeRepository(AppDbContext dbContext) : base(dbContext)
    {
    }
}

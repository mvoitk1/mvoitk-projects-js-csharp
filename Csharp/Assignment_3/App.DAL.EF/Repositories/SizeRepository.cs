using App.Domain;

namespace App.DAL.EF.Repositories;

public class SizeRepository : BaseRepository<Size>, ISizeRepository
{
    public SizeRepository(AppDbContext dbContext) : base(dbContext)
    {
    }
}

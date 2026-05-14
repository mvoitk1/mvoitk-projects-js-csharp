using App.Domain;

namespace App.DAL.EF.Repositories;

public class ColorRepository : BaseRepository<Color>, IColorRepository
{
    public ColorRepository(AppDbContext dbContext) : base(dbContext)
    {
    }
}

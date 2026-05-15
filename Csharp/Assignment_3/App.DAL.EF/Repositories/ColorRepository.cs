using App.DAL.Contracts.Repositories;
using Base.DAL.EF;
using App.Domain;

namespace App.DAL.EF.Repositories;

public class ColorRepository : BaseRepository<Color, AppDbContext>, IColorRepository
{
    public ColorRepository(AppDbContext dbContext) : base(dbContext)
    {
    }
}

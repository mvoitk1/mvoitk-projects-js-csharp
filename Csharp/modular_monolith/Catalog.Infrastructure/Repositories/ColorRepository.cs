using Base.DAL.EF;
using Catalog.Application.Contracts.Repositories;
using Catalog.Domain;

namespace Catalog.Infrastructure.Repositories;

public class ColorRepository(CatalogDbContext dbContext)
    : BaseRepository<Color, CatalogDbContext>(dbContext), IColorRepository
{
}

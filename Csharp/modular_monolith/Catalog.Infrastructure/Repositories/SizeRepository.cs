using Base.DAL.EF;
using Catalog.Application.Contracts.Repositories;
using Catalog.Domain;

namespace Catalog.Infrastructure.Repositories;

public class SizeRepository(CatalogDbContext dbContext)
    : BaseRepository<Size, CatalogDbContext>(dbContext), ISizeRepository
{
}

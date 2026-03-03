using Microsoft.EntityFrameworkCore;
using VenuePlatform.BLL.Domain.Companies;

namespace VenuePlatform.DAL.Persistence;

public sealed class CompanyRepository : ICompanyRepository
{
    private readonly ApplicationDbContext _db;

    public CompanyRepository(ApplicationDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task<Company?> GetBySlugAsync(string slug, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return null;

        var normalized = slug.Trim().ToLowerInvariant();

        return await _db.Companies
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.Slug == normalized, ct);
    }

    public async Task AddAsync(Company company, CancellationToken ct)
    {
        if (company is null)
            throw new ArgumentNullException(nameof(company));

        _db.Companies.Add(company);
        await _db.SaveChangesAsync(ct);
    }
}

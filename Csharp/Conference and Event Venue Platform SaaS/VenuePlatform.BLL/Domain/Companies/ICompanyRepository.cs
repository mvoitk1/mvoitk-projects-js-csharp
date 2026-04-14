namespace VenuePlatform.BLL.Domain.Companies;

public interface ICompanyRepository
{
    Task<Company?> GetBySlugAsync(string slug, CancellationToken cancellationToken);
    Task AddAsync(Company company, CancellationToken cancellationToken);
}

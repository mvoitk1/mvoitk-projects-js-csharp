namespace VenuePlatform.Contracts.Tenancy;

public interface ITenantScoped
{
    Guid CompanyId { get; }
}

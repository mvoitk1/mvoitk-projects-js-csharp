namespace VenuePlatform.DAL.Persistence;

public interface ITenantProvider
{
    Guid? CurrentCompanyId { get; }
}
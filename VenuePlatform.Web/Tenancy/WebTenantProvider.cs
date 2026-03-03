using VenuePlatform.BLL.Tenancy;
using VenuePlatform.DAL.Persistence;

namespace VenuePlatform.Web.Tenancy;

public sealed class WebTenantProvider : ITenantProvider
{
    private readonly ITenantContext _tenantContext;

    public WebTenantProvider(ITenantContext tenantContext)
    {
        _tenantContext = tenantContext;
    }

    public Guid? CurrentCompanyId => _tenantContext.Current?.CompanyId;
}
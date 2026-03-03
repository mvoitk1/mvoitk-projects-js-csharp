using VenuePlatform.BLL.Tenancy;
using VenuePlatform.Contracts.Tenancy;

namespace VenuePlatform.Web.Tenancy;

public class TenantContext : ITenantContext
{
    public TenantInfo? Current { get; internal set; }
}
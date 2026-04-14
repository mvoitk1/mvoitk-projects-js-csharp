using VenuePlatform.Contracts.Tenancy;

namespace VenuePlatform.BLL.Tenancy;

public interface ITenantContext
{
    TenantInfo? Current { get; }
}
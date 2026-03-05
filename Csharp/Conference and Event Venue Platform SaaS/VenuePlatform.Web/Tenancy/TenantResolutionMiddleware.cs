using VenuePlatform.BLL.Domain.Companies;
using VenuePlatform.BLL.Tenancy;
using VenuePlatform.Contracts.Tenancy;

namespace VenuePlatform.Web.Tenancy;

public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ITenantResolver tenantResolver,
        ICompanyRepository companyRepository,
        TenantContext tenantContext)
    {
        var path = context.Request.Path;

        // Allow health and swagger endpoints without tenant
        if (IsExemptPath(path))
        {
            await _next(context);
            return;
        }

        // Resolve slug from path
        var slug = tenantResolver.ResolveSlugFromPath(path.ToString());

        if (slug is null)
        {
            // Invalid or missing tenant slug format
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        // Validate slug against database
        var company = await companyRepository.GetBySlugAsync(slug, context.RequestAborted);

        if (company is null)
        {
            // Valid slug format but company not found => 404
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        // Store tenant in context for this request
        tenantContext.Current = new TenantInfo(company.Id, company.Slug);

        await _next(context);
    }

    private static bool IsExemptPath(PathString path)
    {
        var pathValue = path.Value?.ToLowerInvariant() ?? string.Empty;

        return pathValue == "/health"
            || pathValue.StartsWith("/swagger")
            || pathValue == "/dev"
            || pathValue.StartsWith("/dev/");
    }
}

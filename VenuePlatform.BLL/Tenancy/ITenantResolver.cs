namespace VenuePlatform.BLL.Tenancy;

public interface ITenantResolver
{
    /// <summary>
    /// Extracts and validates the company slug from the request path.
    /// Returns null if the path does not contain a valid slug format.
    /// </summary>
    string? ResolveSlugFromPath(string path);
}

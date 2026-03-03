using VenuePlatform.BLL.Tenancy;

namespace VenuePlatform.Web.Tenancy;

public class PathTenantResolver : ITenantResolver
{
    public string? ResolveSlugFromPath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return null;

        // Extract first non-empty segment
        var segments = path
            .Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (segments.Length == 0)
            return null;

        var slug = segments[0].ToLowerInvariant();

        // Validate slug format: 2-64 chars, lowercase a-z, 0-9, dash only
        if (!IsValidSlug(slug))
            return null;

        return slug;
    }

    private static bool IsValidSlug(string slug)
    {
        if (slug.Length is < 2 or > 64)
            return false;

        foreach (var c in slug)
        {
            if (c is not (>= 'a' and <= 'z' or >= '0' and <= '9' or '-'))
                return false;
        }

        return true;
    }
}

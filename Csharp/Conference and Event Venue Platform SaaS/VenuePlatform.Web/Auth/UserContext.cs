using System.Security.Claims;

namespace VenuePlatform.Web.Auth;

/// <summary>
/// Implementation of <see cref="IUserContext"/> that extracts user identity from the HTTP context.
/// </summary>
public class UserContext : IUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public Guid? UserId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user is null)
            {
                return null;
            }

            // Try ClaimTypes.NameIdentifier first (mapped from JWT "sub" by ASP.NET Core)
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Fallback to "sub" claim if NameIdentifier not found
            if (string.IsNullOrEmpty(userIdClaim))
            {
                userIdClaim = user.FindFirst("sub")?.Value;
            }

            // Parse as Guid; return null if invalid
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return null;
            }

            return userId;
        }
    }

    /// <inheritdoc />
    public bool IsAuthenticated
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            // Authenticated only if UserId is not null AND principal reports authenticated
            return UserId.HasValue && user?.Identity?.IsAuthenticated == true;
        }
    }
}

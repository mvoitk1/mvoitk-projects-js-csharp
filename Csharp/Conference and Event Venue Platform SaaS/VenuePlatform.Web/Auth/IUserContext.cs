namespace VenuePlatform.Web.Auth;

/// <summary>
/// Provides access to the current user's identity information from the HTTP context.
/// Centralizes user-id extraction to eliminate duplicated claim parsing across endpoints.
/// </summary>
public interface IUserContext
{
    /// <summary>
    /// Gets the current user's ID if authenticated; otherwise null.
    /// </summary>
    Guid? UserId { get; }

    /// <summary>
    /// Gets a value indicating whether the current user is authenticated.
    /// </summary>
    bool IsAuthenticated { get; }
}

using Microsoft.AspNetCore.Identity;
using VenuePlatform.Contracts.Auth;
using VenuePlatform.Web.Auth;

namespace VenuePlatform.Web.Endpoints;

/// <summary>
/// Authentication endpoints (non-tenant scoped).
/// </summary>
public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app, IConfiguration configuration)
    {
        // POST /auth/login - Login (no tenant required)
        app.MapPost("/auth/login", async (LoginRequest request, UserManager<IdentityUser<Guid>> userManager, JwtTokenService tokenService) =>
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return Results.BadRequest(new { error = "Email and Password are required." });
            }

            var user = await userManager.FindByEmailAsync(request.Email);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var isPasswordValid = await userManager.CheckPasswordAsync(user, request.Password);
            if (!isPasswordValid)
            {
                return Results.Unauthorized();
            }

            var token = tokenService.GenerateToken(user);
            var expiresAt = DateTime.UtcNow.AddMinutes(int.Parse(configuration["Jwt:ExpiresMinutes"]!));

            return Results.Ok(new LoginResponse(token, expiresAt, user.Email!));
        });

        return app;
    }
}

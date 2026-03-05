using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VenuePlatform.BLL.Domain.Auth;
using VenuePlatform.BLL.Domain.Companies;
using VenuePlatform.Contracts.Auth;
using VenuePlatform.DAL.Persistence;
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

        // POST /auth/register - Register new user (Owner or User mode)
        app.MapPost("/auth/register", async (
            RegisterRequest request,
            UserManager<IdentityUser<Guid>> userManager,
            ApplicationDbContext dbContext) =>
        {
            // Basic validation
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return Results.BadRequest(new { error = "Email and Password are required." });
            }

            if (string.IsNullOrWhiteSpace(request.Mode) ||
                !(request.Mode.Equals("Owner", StringComparison.OrdinalIgnoreCase) ||
                  request.Mode.Equals("User", StringComparison.OrdinalIgnoreCase)))
            {
                return Results.BadRequest(new { error = "Mode must be 'Owner' or 'User'." });
            }

            if (string.IsNullOrWhiteSpace(request.CompanySlug))
            {
                return Results.BadRequest(new { error = "CompanySlug is required." });
            }

            var normalizedSlug = request.CompanySlug.Trim().ToLowerInvariant();
            var isOwnerMode = request.Mode.Equals("Owner", StringComparison.OrdinalIgnoreCase);

            // Owner mode requires CompanyName
            if (isOwnerMode && string.IsNullOrWhiteSpace(request.CompanyName))
            {
                return Results.BadRequest(new { error = "CompanyName is required for Owner registration." });
            }

            // Check if email already exists
            var existingUser = await userManager.FindByEmailAsync(request.Email);
            if (existingUser is not null)
            {
                return Results.Conflict(new { error = "Email already registered." });
            }

            // Execute in transaction for atomicity
            await using var transaction = await dbContext.Database.BeginTransactionAsync();

            try
            {
                Guid companyId;
                string membershipRole;

                if (isOwnerMode)
                {
                    // Owner mode: create new company
                    var existingCompany = await dbContext.Companies
                        .AsNoTracking()
                        .FirstOrDefaultAsync(c => c.Slug == normalizedSlug);

                    if (existingCompany is not null)
                    {
                        return Results.Conflict(new { error = "Company slug already exists." });
                    }

                    var company = new Company(request.CompanyName!, normalizedSlug);
                    dbContext.Companies.Add(company);
                    await dbContext.SaveChangesAsync();

                    companyId = company.Id;
                    membershipRole = TenantRoles.CompanyOwner;
                }
                else
                {
                    // User mode: join existing company
                    var company = await dbContext.Companies
                        .AsNoTracking()
                        .FirstOrDefaultAsync(c => c.Slug == normalizedSlug);

                    if (company is null)
                    {
                        return Results.NotFound(new { error = "Company not found." });
                    }

                    companyId = company.Id;
                    // Default to Employee; ignore any requested role from public signup
                    membershipRole = TenantRoles.CompanyEmployee;
                }

                // Create Identity user
                var user = new IdentityUser<Guid>
                {
                    Id = Guid.NewGuid(),
                    UserName = request.Email,
                    Email = request.Email,
                    EmailConfirmed = true // Auto-confirm for now (no email verification)
                };

                var createResult = await userManager.CreateAsync(user, request.Password);
                if (!createResult.Succeeded)
                {
                    var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
                    return Results.BadRequest(new { error = $"Failed to create user: {errors}" });
                }

                // Create membership
                var membership = new UserCompanyMembership(user.Id, companyId, membershipRole);
                dbContext.UserCompanyMemberships.Add(membership);
                await dbContext.SaveChangesAsync();

                await transaction.CommitAsync();

                var response = new RegisterResponse(
                    user.Id,
                    user.Email!,
                    normalizedSlug,
                    membershipRole
                );

                return Results.Created($"/auth/users/{user.Id}", response);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Results.Problem($"Registration failed: {ex.Message}");
            }
        });

        return app;
    }
}

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VenuePlatform.BLL.Domain.Auth;
using VenuePlatform.BLL.Domain.Companies;
using VenuePlatform.Contracts.Auth;
using VenuePlatform.DAL.Persistence;
using VenuePlatform.Web.Auth;
using Microsoft.AspNetCore.Authorization;

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

            return Results.Ok(new LoginResponse(token, expiresAt, user.Email!, user.Id));
        });

        // POST /auth/register - Register new user (account-only, Owner, or User mode)
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

            // Check if email already exists
            var existingUser = await userManager.FindByEmailAsync(request.Email);
            if (existingUser is not null)
            {
                return Results.Conflict(new { error = "Email already registered." });
            }

            // Determine mode: null/empty = account-only signup
            var isAccountOnly = string.IsNullOrWhiteSpace(request.Mode);
            var isOwnerMode = !isAccountOnly && request.Mode!.Equals("Owner", StringComparison.OrdinalIgnoreCase);
            var isUserMode = !isAccountOnly && request.Mode!.Equals("User", StringComparison.OrdinalIgnoreCase);

            // Validate mode if provided
            if (!isAccountOnly && !isOwnerMode && !isUserMode)
            {
                return Results.BadRequest(new { error = "Mode must be 'Owner', 'User', or omitted for account-only signup." });
            }

            // Validate company fields based on mode
            if (isOwnerMode && string.IsNullOrWhiteSpace(request.CompanyName))
            {
                return Results.BadRequest(new { error = "CompanyName is required for Owner registration." });
            }

            if ((isOwnerMode || isUserMode) && string.IsNullOrWhiteSpace(request.CompanySlug))
            {
                return Results.BadRequest(new { error = "CompanySlug is required for Owner or User registration." });
            }

            string? normalizedSlug = isAccountOnly ? null : request.CompanySlug!.Trim().ToLowerInvariant();

            // Execute in transaction for atomicity
            await using var transaction = await dbContext.Database.BeginTransactionAsync();

            try
            {
                Guid? companyId = null;
                string? membershipRole = null;

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

                    var company = new Company(request.CompanyName!, normalizedSlug!);
                    dbContext.Companies.Add(company);
                    await dbContext.SaveChangesAsync();

                    companyId = company.Id;
                    membershipRole = TenantRoles.CompanyOwner;
                }
                else if (isUserMode)
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
                // Account-only mode: companyId and membershipRole remain null

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

                // Create membership only if companyId is set (Owner or User mode)
                if (companyId.HasValue && membershipRole is not null)
                {
                    var membership = new UserCompanyMembership(user.Id, companyId.Value, membershipRole);
                    dbContext.UserCompanyMemberships.Add(membership);
                    await dbContext.SaveChangesAsync();
                }

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

        // GET /auth/me - Get current user info with company memberships (requires auth)
        app.MapGet("/auth/me", [Authorize] async (
            IUserContext userContext,
            UserManager<IdentityUser<Guid>> userManager,
            ApplicationDbContext dbContext) =>
        {
            var userId = userContext.UserId;
            if (!userId.HasValue)
            {
                return Results.Unauthorized();
            }

            var user = await userManager.FindByIdAsync(userId.Value.ToString());
            if (user is null)
            {
                return Results.Unauthorized();
            }

            // Query memberships joined with companies
            var memberships = await dbContext.UserCompanyMemberships
                .AsNoTracking()
                .Where(m => m.UserId == userId.Value)
                .Join(
                    dbContext.Companies.AsNoTracking(),
                    m => m.CompanyId,
                    c => c.Id,
                    (m, c) => new UserCompanyDto(c.Slug, c.Name, m.Role))
                .ToListAsync();

            var response = new MeResponse(
                userId.Value,
                user.Email!,
                memberships.Count > 0,
                memberships
            );

            return Results.Ok(response);
        });

        return app;
    }
}

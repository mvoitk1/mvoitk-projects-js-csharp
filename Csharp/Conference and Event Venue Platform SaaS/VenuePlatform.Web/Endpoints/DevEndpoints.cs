using Microsoft.AspNetCore.Identity;
using VenuePlatform.BLL.Domain.Auth;
using VenuePlatform.BLL.Domain.Companies;
using VenuePlatform.Contracts.Auth;
using VenuePlatform.Contracts.Companies;
using VenuePlatform.DAL.Persistence;

namespace VenuePlatform.Web.Endpoints;

/// <summary>
/// Development-only endpoints (no auth required). These should only be mapped in Development environment.
/// </summary>
public static class DevEndpoints
{
    public static IEndpointRouteBuilder MapDevEndpoints(this IEndpointRouteBuilder app)
    {
        // DEV-ONLY: Create company endpoint (no auth)
        app.MapPost("/dev/companies", async (CreateCompanyRequest request, ICompanyRepository repo, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Slug))
            {
                return Results.BadRequest(new { error = "Name and Slug are required." });
            }

            // Normalize and validate slug
            var slug = request.Slug.Trim().ToLowerInvariant();
            if (slug.Length < 2 || slug.Length > 64)
            {
                return Results.BadRequest(new { error = "Slug must be 2-64 chars and contain only a-z, 0-9, and '-" });
            }
            foreach (var c in slug)
            {
                if (!char.IsAsciiLetterLower(c) && !char.IsAsciiDigit(c) && c != '-')
                {
                    return Results.BadRequest(new { error = "Slug must be 2-64 chars and contain only a-z, 0-9, and '-" });
                }
            }

            var existing = await repo.GetBySlugAsync(slug, ct);
            if (existing is not null)
            {
                return Results.Conflict(new { error = $"Company with slug '{slug}' already exists." });
            }

            var company = new Company(request.Name, slug);
            await repo.AddAsync(company, ct);

            return Results.Created($"/dev/companies/{company.Id}", new { company.Id, company.Name, company.Slug });
        });

        // DEV-ONLY: Create user endpoint (no auth)
        app.MapPost("/dev/users", async (UserManager<IdentityUser<Guid>> userManager, CreateUserRequest request) =>
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return Results.BadRequest(new { error = "Email and Password are required." });
            }

            var user = new IdentityUser<Guid>
            {
                UserName = request.Email,
                Email = request.Email
            };

            var result = await userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                return Results.BadRequest(new { error = string.Join(", ", result.Errors.Select(e => e.Description)) });
            }

            return Results.Created($"/dev/users/{user.Id}", new { user.Id, user.Email });
        });

        // DEV-ONLY: Assign user to company with role
        app.MapPost("/dev/memberships", async (ApplicationDbContext db, AssignMembershipRequest request) =>
        {
            if (request.UserId == Guid.Empty || request.CompanyId == Guid.Empty || string.IsNullOrWhiteSpace(request.Role))
            {
                return Results.BadRequest(new { error = "UserId, CompanyId, and Role are required." });
            }

            // Validate role is a known tenant role
            var validRoles = new[] { TenantRoles.CompanyOwner, TenantRoles.CompanyAdmin, TenantRoles.CompanyManager, TenantRoles.CompanyEmployee };
            if (!validRoles.Contains(request.Role))
            {
                return Results.BadRequest(new { error = $"Role must be one of: {string.Join(", ", validRoles)}" });
            }

            // Check for duplicate
            var existing = db.UserCompanyMemberships
                .FirstOrDefault(m => m.UserId == request.UserId && m.CompanyId == request.CompanyId);

            if (existing is not null)
            {
                return Results.Conflict(new { error = "User is already a member of this company." });
            }

            var membership = new UserCompanyMembership(request.UserId, request.CompanyId, request.Role);
            db.UserCompanyMemberships.Add(membership);
            await db.SaveChangesAsync();

            return Results.Created($"/dev/memberships/{membership.UserId}/{membership.CompanyId}", new
            {
                membership.UserId,
                membership.CompanyId,
                membership.Role
            });
        });

        return app;
    }
}

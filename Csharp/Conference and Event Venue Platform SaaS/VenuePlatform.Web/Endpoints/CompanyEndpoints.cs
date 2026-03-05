using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VenuePlatform.BLL.Domain.Auth;
using VenuePlatform.BLL.Domain.Companies;
using VenuePlatform.Contracts.Auth;
using VenuePlatform.Contracts.Companies;
using VenuePlatform.Contracts.Spaces;
using VenuePlatform.DAL.Persistence;
using VenuePlatform.Web.Auth;

namespace VenuePlatform.Web.Endpoints;

/// <summary>
/// Company endpoints (global - not tenant scoped).
/// </summary>
public static class CompanyEndpoints
{
    public static IEndpointRouteBuilder MapCompanyEndpoints(this IEndpointRouteBuilder app)
    {
        // GET /companies/public - List all public companies (venues) - PUBLIC, no auth required
        app.MapGet("/companies/public", async (ApplicationDbContext dbContext) =>
        {
            var companies = await dbContext.Companies
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .Select(c => new PublicCompanyDto(c.Slug, c.Name, c.Plan))
                .ToListAsync();

            var response = new PublicCompaniesResponse(companies);
            return Results.Ok(response);
        }).AllowAnonymous();

        // GET /companies/{companySlug}/spaces/public - List public spaces for a venue - PUBLIC, no auth required
        app.MapGet("/companies/{companySlug}/spaces/public", async (
            string companySlug,
            ApplicationDbContext dbContext) =>
        {
            // Resolve company by slug
            var company = await dbContext.Companies
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Slug == companySlug.ToLowerInvariant());

            if (company is null)
            {
                return Results.NotFound(new { error = "Company not found." });
            }

            // Get spaces for this company (bypass tenant filter by using IgnoreQueryFilters)
            var spaces = await dbContext.Spaces
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(s => s.CompanyId == company.Id && s.IsActive)
                .OrderBy(s => s.Name)
                .Select(s => new PublicSpaceDto(s.Id, s.Name, s.Capacity, s.Notes))
                .ToListAsync();

            return Results.Ok(spaces);
        }).AllowAnonymous();

        // POST /companies - Create a new company (requires auth)
        app.MapPost("/companies", [Authorize] async (
            CreateCompanyRequest request,
            IUserContext userContext,
            ApplicationDbContext dbContext) =>
        {
            var userId = userContext.UserId;
            if (!userId.HasValue)
            {
                return Results.Unauthorized();
            }

            // Validate request
            if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Slug))
            {
                return Results.BadRequest(new { error = "CompanyName and CompanySlug are required." });
            }

            var normalizedSlug = request.Slug.Trim().ToLowerInvariant();

            // Check slug uniqueness
            var existingCompany = await dbContext.Companies
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Slug == normalizedSlug);

            if (existingCompany is not null)
            {
                return Results.Conflict(new { error = "Company slug already exists." });
            }

            // Execute in transaction for atomicity
            await using var transaction = await dbContext.Database.BeginTransactionAsync();

            try
            {
                // Create company
                var company = new Company(request.Name.Trim(), normalizedSlug);
                dbContext.Companies.Add(company);
                await dbContext.SaveChangesAsync();

                // Create membership for current user as CompanyOwner
                var membership = new UserCompanyMembership(userId.Value, company.Id, TenantRoles.CompanyOwner);
                dbContext.UserCompanyMemberships.Add(membership);
                await dbContext.SaveChangesAsync();

                await transaction.CommitAsync();

                var response = new CreateCompanyResponse(
                    company.Id,
                    company.Name,
                    company.Slug,
                    TenantRoles.CompanyOwner
                );

                return Results.Created($"/companies/{company.Slug}", response);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Results.Problem($"Failed to create company: {ex.Message}");
            }
        });

        return app;
    }
}

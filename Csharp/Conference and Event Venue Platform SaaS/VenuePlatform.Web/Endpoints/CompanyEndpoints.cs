using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VenuePlatform.BLL.Domain.Bookings;
using VenuePlatform.BLL.Domain.Auth;
using VenuePlatform.BLL.Domain.Clients;
using VenuePlatform.BLL.Domain.Companies;
using VenuePlatform.Contracts.Auth;
using VenuePlatform.Contracts.BookingRequests;
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

        // POST /companies/{companySlug}/booking-requests - Create customer booking request (requires auth, no membership required)
        app.MapPost("/companies/{companySlug}/booking-requests", async (
            string companySlug,
            CreateBookingRequestRequest request,
            IUserContext userContext,
            ApplicationDbContext dbContext) =>
        {
            var userId = userContext.UserId;
            if (!userId.HasValue)
            {
                return Results.Unauthorized();
            }

            if (string.IsNullOrWhiteSpace(request.ContactName))
            {
                return Results.BadRequest(new { error = "ContactName is required." });
            }

            if (string.IsNullOrWhiteSpace(request.ContactEmail))
            {
                return Results.BadRequest(new { error = "ContactEmail is required." });
            }

            if (request.EndUtc <= request.StartUtc)
            {
                return Results.BadRequest(new { error = "EndUtc must be after StartUtc." });
            }

            if (request.SpaceIds is null || request.SpaceIds.Count == 0)
            {
                return Results.BadRequest(new { error = "SpaceIds must contain at least one space." });
            }

            var normalizedSlug = companySlug.Trim().ToLowerInvariant();
            var company = await dbContext.Companies
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Slug == normalizedSlug);

            if (company is null)
            {
                return Results.NotFound(new { error = "Company not found." });
            }

            var dedupedSpaceIds = request.SpaceIds.Distinct().ToList();
            var validSpaceIds = await dbContext.Spaces
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(s => s.CompanyId == company.Id && s.IsActive && dedupedSpaceIds.Contains(s.Id))
                .Select(s => s.Id)
                .ToListAsync();

            if (validSpaceIds.Count != dedupedSpaceIds.Count)
            {
                return Results.BadRequest(new { error = "One or more selected spaces are invalid or inactive for this company." });
            }

            var normalizedEmail = request.ContactEmail.Trim();
            var client = await dbContext.Clients
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(c =>
                    c.CompanyId == company.Id &&
                    c.Email != null &&
                    c.Email.ToLower() == normalizedEmail.ToLower());

            if (client is null)
            {
                client = new Client(company.Id, request.ContactName.Trim(), request.Notes?.Trim());
                client.UpdateEmail(normalizedEmail);
                dbContext.Clients.Add(client);
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(request.ContactName) &&
                    !string.Equals(client.Name, request.ContactName.Trim(), StringComparison.Ordinal))
                {
                    client.UpdateName(request.ContactName.Trim());
                }

                if (!string.IsNullOrWhiteSpace(request.Notes))
                {
                    client.SetNotes(request.Notes.Trim());
                }
            }

            var booking = new Booking(
                company.Id,
                client.Id,
                $"Booking request from {request.ContactName.Trim()}",
                request.StartUtc,
                request.EndUtc,
                0);

            booking.SetCreatedBy(userId.Value);
            booking.SetTotalAmount(0);
            dbContext.Bookings.Add(booking);

            foreach (var spaceId in dedupedSpaceIds)
            {
                dbContext.BookingSpaces.Add(new BookingSpace
                {
                    BookingId = booking.Id,
                    SpaceId = spaceId
                });
            }

            await dbContext.SaveChangesAsync();

            return Results.Created(
                $"/companies/{company.Slug}/booking-requests/{booking.Id}",
                new CreateBookingRequestResponse(booking.Id));
        })
        .RequireAuthorization();

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

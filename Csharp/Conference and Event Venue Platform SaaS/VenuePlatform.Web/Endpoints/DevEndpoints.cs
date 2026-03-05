using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Routing;
using VenuePlatform.BLL.Domain.Auth;
using VenuePlatform.BLL.Domain.Companies;
using VenuePlatform.Contracts.Auth;
using VenuePlatform.Contracts.Companies;
using VenuePlatform.DAL.Persistence;

namespace VenuePlatform.Web.Endpoints;

/// <summary>
/// Request model for dev bootstrap endpoint (JSON body).
/// </summary>
public record CreateBootstrapRequest(
    string CompanyName,
    string CompanySlug,
    string Email,
    string Password,
    string Role
);

/// <summary>
/// Development-only endpoints (no auth required). These should only be mapped in Development environment.
/// </summary>
public static class DevEndpoints
{
    public static IEndpointRouteBuilder MapDevEndpoints(this IEndpointRouteBuilder app)
    {
        // DEV-ONLY: List all registered routes (diagnostic)
        app.MapGet("/dev/routes", (EndpointDataSource endpointDataSource) =>
        {
            var routes = endpointDataSource.Endpoints
                .OfType<RouteEndpoint>()
                .Select(endpoint => new
                {
                    Pattern = endpoint.RoutePattern.RawText,
                    Methods = endpoint.Metadata
                        .OfType<HttpMethodMetadata>()
                        .FirstOrDefault()?.HttpMethods ?? new List<string> { "N/A" }
                })
                .OrderBy(r => r.Pattern)
                .ToList();

            return Results.Json(routes);
        })
        .WithName("DevRoutes")
        .WithDescription("Development-only: List all registered routes.");

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

        // DEV-ONLY: Bootstrap tenant (create company + user + membership in one call)
        app.MapPost("/dev/bootstrap", async (
            [FromBody] CreateBootstrapRequest request,
            UserManager<IdentityUser<Guid>> userManager,
            ApplicationDbContext db,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.CompanyName) || string.IsNullOrWhiteSpace(request.CompanySlug))
            {
                return Results.BadRequest(new { code = "validation_error", error = "Company name and slug are required." });
            }

            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return Results.BadRequest(new { code = "validation_error", error = "Email and password are required." });
            }

            // Validate role
            var validRoles = new[] { TenantRoles.CompanyOwner, TenantRoles.CompanyAdmin, TenantRoles.CompanyManager, TenantRoles.CompanyEmployee };
            var normalizedRole = string.IsNullOrWhiteSpace(request.Role) ? TenantRoles.CompanyOwner : request.Role;
            if (!validRoles.Contains(normalizedRole))
            {
                return Results.BadRequest(new { code = "validation_error", error = $"Role must be one of: {string.Join(", ", validRoles)}" });
            }

            // Normalize and validate slug
            var slug = request.CompanySlug.Trim().ToLowerInvariant();
            if (slug.Length < 2 || slug.Length > 64)
            {
                return Results.BadRequest(new { code = "validation_error", error = "Slug must be 2-64 chars and contain only a-z, 0-9, and '-'" });
            }
            foreach (var c in slug)
            {
                if (!char.IsAsciiLetterLower(c) && !char.IsAsciiDigit(c) && c != '-')
                {
                    return Results.BadRequest(new { code = "validation_error", error = "Slug must be 2-64 chars and contain only a-z, 0-9, and '-'" });
                }
            }

            // Check if company already exists
            var existingCompany = await db.Companies
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Slug == slug, ct);
            if (existingCompany is not null)
            {
                return Results.Conflict(new { code = "conflict", error = $"Company with slug '{slug}' already exists." });
            }

            // Create company
            var company = new Company(request.CompanyName, slug);
            db.Companies.Add(company);
            await db.SaveChangesAsync(ct);

            // Create user
            var user = new IdentityUser<Guid>
            {
                UserName = request.Email,
                Email = request.Email
            };

            var userResult = await userManager.CreateAsync(user, request.Password);
            if (!userResult.Succeeded)
            {
                // Rollback: delete the company we just created
                db.Companies.Remove(company);
                await db.SaveChangesAsync(ct);
                return Results.BadRequest(new { code = "validation_error", error = string.Join(", ", userResult.Errors.Select(e => e.Description)) });
            }

            // Create membership
            var membership = new UserCompanyMembership(user.Id, company.Id, normalizedRole);
            db.UserCompanyMemberships.Add(membership);
            await db.SaveChangesAsync(ct);

            return Results.Ok(new
            {
                companyId = company.Id,
                userId = user.Id,
                companySlug = slug,
                role = normalizedRole
            });
        })
        .WithName("DevBootstrap")
        .WithDescription("Development-only: Create a company, user, and membership in one call.")
        .DisableAntiforgery();

        // DEV-ONLY: Export tenant diagnostics as CSV
        app.MapGet("/dev/companies/{companySlug}/diagnostics.csv", async (
            string companySlug,
            ApplicationDbContext db,
            CancellationToken ct) =>
        {
            // Validate slug format (same as tenant resolver)
            if (!IsValidSlug(companySlug))
            {
                return Results.BadRequest(new { error = "Invalid company slug format." });
            }

            var slug = companySlug.ToLowerInvariant();

            // Load company by slug (ignore tenant filter for dev endpoint)
            var company = await db.Companies
                .AsNoTracking()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.Slug == slug, ct);

            if (company is null)
            {
                return Results.NotFound(new { error = $"Company with slug '{slug}' not found." });
            }

            var companyId = company.Id;
            var now = DateTime.UtcNow;
            var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var endOfMonth = startOfMonth.AddMonths(1);

            // Get counts
            var spaceCount = await db.Spaces
                .AsNoTracking()
                .IgnoreQueryFilters()
                .CountAsync(s => s.CompanyId == companyId, ct);

            var bookingCountThisMonth = await db.Bookings
                .AsNoTracking()
                .IgnoreQueryFilters()
                .CountAsync(b => b.CompanyId == companyId && b.CreatedUtc >= startOfMonth && b.CreatedUtc < endOfMonth, ct);

            var invoiceCountThisMonth = await db.Invoices
                .AsNoTracking()
                .IgnoreQueryFilters()
                .CountAsync(i => i.CompanyId == companyId && i.CreatedUtc >= startOfMonth && i.CreatedUtc < endOfMonth, ct);

            // Get latest 20 bookings
            var recentBookings = await db.Bookings
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(b => b.CompanyId == companyId)
                .OrderByDescending(b => b.CreatedUtc)
                .Take(20)
                .Select(b => new
                {
                    b.Id,
                    b.Title,
                    b.StartUtc,
                    b.EndUtc,
                    b.Status,
                    b.IsCancelled,
                    b.TotalAmount,
                    b.CreatedUtc
                })
                .ToListAsync(ct);

            // Get latest 20 invoices
            var recentInvoices = await db.Invoices
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(i => i.CompanyId == companyId)
                .OrderByDescending(i => i.CreatedUtc)
                .Take(20)
                .Select(i => new
                {
                    i.Id,
                    i.InvoiceNumberText,
                    i.Status,
                    i.SubtotalAmount,
                    i.CreatedUtc,
                    i.IssuedUtc,
                    i.SentUtc,
                    i.PaidUtc,
                    i.VoidedUtc
                })
                .ToListAsync(ct);

            // Build CSV
            var csv = new StringBuilder();

            // [Company] section
            csv.AppendLine("[Company]");
            csv.AppendLine($"CompanyId,{EscapeCsv(companyId.ToString())}");
            csv.AppendLine($"Slug,{EscapeCsv(slug)}");
            csv.AppendLine($"Name,{EscapeCsv(company.Name)}");
            csv.AppendLine();

            // [Counts] section
            csv.AppendLine("[Counts]");
            csv.AppendLine($"SpaceCount,{spaceCount}");
            csv.AppendLine($"BookingCountThisMonth,{bookingCountThisMonth}");
            csv.AppendLine($"InvoiceCountThisMonth,{invoiceCountThisMonth}");
            csv.AppendLine();

            // [RecentBookings] section
            csv.AppendLine("[RecentBookings]");
            csv.AppendLine("Id,Title,StartUtc,EndUtc,Status,IsCancelled,TotalAmount,CreatedUtc");
            foreach (var b in recentBookings)
            {
                csv.AppendLine($"{EscapeCsv(b.Id.ToString())},{EscapeCsv(b.Title)},{EscapeCsv(b.StartUtc.ToString("O"))},{EscapeCsv(b.EndUtc.ToString("O"))},{EscapeCsv(b.Status.ToString())},{b.IsCancelled},{b.TotalAmount.ToString(CultureInfo.InvariantCulture)},{EscapeCsv(b.CreatedUtc.ToString("O"))}");
            }
            csv.AppendLine();

            // [RecentInvoices] section
            csv.AppendLine("[RecentInvoices]");
            csv.AppendLine("Id,InvoiceNumberText,Status,SubtotalAmount,CreatedUtc,IssuedUtc,SentUtc,PaidUtc,VoidedUtc");
            foreach (var i in recentInvoices)
            {
                csv.AppendLine($"{EscapeCsv(i.Id.ToString())},{EscapeCsv(i.InvoiceNumberText)},{EscapeCsv(i.Status.ToString())},{i.SubtotalAmount.ToString(CultureInfo.InvariantCulture)},{EscapeCsv(i.CreatedUtc.ToString("O"))},{EscapeCsv(i.IssuedUtc?.ToString("O"))},{EscapeCsv(i.SentUtc?.ToString("O"))},{EscapeCsv(i.PaidUtc?.ToString("O"))},{EscapeCsv(i.VoidedUtc?.ToString("O"))}");
            }

            var csvBytes = Encoding.UTF8.GetBytes(csv.ToString());
            var filename = $"tenant-diagnostics-{slug}.csv";

            return Results.File(csvBytes, "text/csv", filename);
        });

        return app;
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

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        // If contains comma or quote, wrap in quotes and escape quotes
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        return value;
    }
}

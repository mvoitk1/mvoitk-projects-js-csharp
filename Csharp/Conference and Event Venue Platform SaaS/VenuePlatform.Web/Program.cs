using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using VenuePlatform.BLL.Domain.Auth;
using VenuePlatform.BLL.Domain.Bookings;
using VenuePlatform.BLL.Domain.Clients;
using VenuePlatform.BLL.Domain.Companies;
using VenuePlatform.BLL.Domain.Spaces;
using VenuePlatform.BLL.Tenancy;
using VenuePlatform.Contracts.Auth;
using VenuePlatform.Contracts.Bookings;
using VenuePlatform.Contracts.Companies;
using VenuePlatform.Contracts.Spaces;
using VenuePlatform.DAL.Persistence;
using VenuePlatform.Web.Auth;
using VenuePlatform.Web.Tenancy;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Tenant resolution services
builder.Services.AddSingleton<ITenantResolver, PathTenantResolver>();
builder.Services.AddScoped<ITenantContext, TenantContext>();
builder.Services.AddScoped<TenantContext>(sp => (TenantContext)sp.GetRequiredService<ITenantContext>());
builder.Services.AddScoped<ITenantProvider, WebTenantProvider>();

// EF Core DbContext
var connectionString = builder.Configuration.GetConnectionString("Default");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

// ASP.NET Core Identity
builder.Services
    .AddIdentityCore<IdentityUser<Guid>>(options =>
    {
        options.User.RequireUniqueEmail = true;
    })
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var jwtKey = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(jwtKey),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// Repositories
builder.Services.AddScoped<ICompanyRepository, CompanyRepository>();

// Auth services
builder.Services.AddScoped<JwtTokenService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

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
}

app.UseHttpsRedirection();

// Authentication & Authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// Tenant resolution middleware - must be before endpoints
app.UseMiddleware<TenantResolutionMiddleware>();

// Health endpoint without tenant (exempt from middleware check)
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTimeOffset.UtcNow }));

// Auth endpoint - Login (no tenant required)
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
    var expiresAt = DateTime.UtcNow.AddMinutes(int.Parse(builder.Configuration["Jwt:ExpiresMinutes"]!));

    return Results.Ok(new LoginResponse(token, expiresAt, user.Email!));
});

// Tenant route group - endpoints under /{companySlug}
var tenantGroup = app.MapGroup("/{companySlug}");

// Health endpoint with tenant slug
tenantGroup.MapGet("/health", (ITenantContext tenantContext) =>
{
    var tenant = tenantContext.Current;
    return tenant is null
        ? Results.Problem("Tenant not resolved", statusCode: 500)
        : Results.Ok(new { companyId = tenant.CompanyId, companySlug = tenant.CompanySlug });
});

// Verification endpoint for DbContext/Repository wiring (pure DI check - no DB access)
tenantGroup.MapGet("/db-check", (ICompanyRepository repo, ITenantContext tenantContext) =>
{
    var tenant = tenantContext.Current;
    return tenant is null
        ? Results.NotFound()
        : Results.Ok(new { companyId = tenant.CompanyId, companySlug = tenant.CompanySlug, repoResolved = true, timestamp = DateTimeOffset.UtcNow });
});

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

tenantGroup.MapGet("/weatherforecast", (ITenantContext tenantContext) =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return Results.Ok(new { companyId = tenantContext.Current?.CompanyId, companySlug = tenantContext.Current?.CompanySlug, forecast });
})
.WithName("GetWeatherForecast");

// Client endpoints for tenant isolation verification

// POST /{companySlug}/clients/seed-one - Creates a test client for current tenant (requires auth + manager role)
tenantGroup.MapPost("/clients/seed-one", (ApplicationDbContext db, ITenantContext tenantContext, ClaimsPrincipal user) =>
{
    // Extract userId from "sub" claim
    var userIdClaim = user.FindFirst("sub")?.Value;
    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
    {
        return Results.Unauthorized();
    }

    var tenant = tenantContext.Current;
    if (tenant is null)
    {
        return Results.NotFound();
    }

    // Query membership with role
    var membership = db.UserCompanyMemberships
        .AsNoTracking()
        .FirstOrDefault(m => m.UserId == userId && m.CompanyId == tenant.CompanyId);

    if (membership is null)
    {
        return Results.Forbid();
    }

    // Allow only: CompanyOwner, CompanyAdmin, CompanyManager
    var allowedRoles = new[] { TenantRoles.CompanyOwner, TenantRoles.CompanyAdmin, TenantRoles.CompanyManager };
    if (!allowedRoles.Contains(membership.Role))
    {
        return Results.Forbid();
    }

    var client = new Client(tenant.CompanyId, "First Client", "Seeded");
    db.Clients.Add(client);
    db.SaveChanges();

    return Results.Created($"/{tenant.CompanySlug}/clients/{client.Id}", new { id = client.Id });
})
.RequireAuthorization();

// GET /{companySlug}/clients - Lists clients for current tenant (requires auth + membership)
tenantGroup.MapGet("/clients", (ApplicationDbContext db, ITenantContext tenantContext, ClaimsPrincipal user) =>
{
    // Extract userId from "sub" claim
    var userIdClaim = user.FindFirst("sub")?.Value;
    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
    {
        return Results.Unauthorized();
    }

    var tenant = tenantContext.Current;
    if (tenant is null)
    {
        return Results.NotFound();
    }

    // Check membership
    var isMember = db.UserCompanyMemberships
        .AsNoTracking()
        .Any(m => m.UserId == userId && m.CompanyId == tenant.CompanyId);

    if (!isMember)
    {
        return Results.Forbid();
    }

    var clients = db.Clients
        .AsNoTracking()
        .OrderBy(c => c.Name)
        .Select(c => new { c.Id, c.Name, c.CompanyId })
        .ToList();

    return Results.Ok(clients);
})
.RequireAuthorization();

// GET /{companySlug}/memberships - Lists user memberships for current tenant (query by CompanyId)
tenantGroup.MapGet("/memberships", (ApplicationDbContext db, ITenantContext tenantContext) =>
{
    var tenant = tenantContext.Current;
    if (tenant is null)
    {
        return Results.NotFound();
    }

    var memberships = db.UserCompanyMemberships
        .AsNoTracking()
        .Where(m => m.CompanyId == tenant.CompanyId)
        .OrderBy(m => m.Role)
        .Select(m => new { m.UserId, m.CompanyId, m.Role, m.CreatedUtc })
        .ToList();

    return Results.Ok(memberships);
});

// Space endpoints for tenant isolation verification

// GET /{companySlug}/spaces - Lists spaces for current tenant (requires auth + membership)
tenantGroup.MapGet("/spaces", (ApplicationDbContext db, ITenantContext tenantContext, ClaimsPrincipal user) =>
{
    // Extract userId from "sub" claim
    var userIdClaim = user.FindFirst("sub")?.Value;
    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
    {
        return Results.Unauthorized();
    }

    var tenant = tenantContext.Current;
    if (tenant is null)
    {
        return Results.NotFound();
    }

    // Check membership
    var isMember = db.UserCompanyMemberships
        .AsNoTracking()
        .Any(m => m.UserId == userId && m.CompanyId == tenant.CompanyId);

    if (!isMember)
    {
        return Results.Forbid();
    }

    var spaces = db.Spaces
        .AsNoTracking()
        .OrderBy(s => s.Name)
        .Select(s => new { s.Id, s.Name, s.Capacity, s.HourlyRate, s.CompanyId })
        .ToList();

    return Results.Ok(spaces);
})
.RequireAuthorization();

// POST /{companySlug}/spaces - Creates a new space for current tenant (requires auth + Manager/Admin/Owner)
tenantGroup.MapPost("/spaces", (ApplicationDbContext db, ITenantContext tenantContext, ClaimsPrincipal user, CreateSpaceRequest request) =>
{
    // Extract userId from "sub" claim
    var userIdClaim = user.FindFirst("sub")?.Value;
    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
    {
        return Results.Unauthorized();
    }

    var tenant = tenantContext.Current;
    if (tenant is null)
    {
        return Results.NotFound();
    }

    // Query membership with role
    var membership = db.UserCompanyMemberships
        .AsNoTracking()
        .FirstOrDefault(m => m.UserId == userId && m.CompanyId == tenant.CompanyId);

    if (membership is null)
    {
        return Results.Forbid();
    }

    // Allow only: CompanyOwner, CompanyAdmin, CompanyManager
    var allowedRoles = new[] { TenantRoles.CompanyOwner, TenantRoles.CompanyAdmin, TenantRoles.CompanyManager };
    if (!allowedRoles.Contains(membership.Role))
    {
        return Results.Forbid();
    }

    // Validate request
    if (string.IsNullOrWhiteSpace(request.Name))
    {
        return Results.BadRequest(new { error = "Name is required." });
    }
    if (request.Capacity <= 0)
    {
        return Results.BadRequest(new { error = "Capacity must be greater than 0." });
    }
    if (request.HourlyRate < 0)
    {
        return Results.BadRequest(new { error = "Hourly rate cannot be negative." });
    }

    var space = new Space(tenant.CompanyId, request.Name.Trim(), request.Capacity, request.HourlyRate);
    db.Spaces.Add(space);
    db.SaveChanges();

    return Results.Created($"/{tenant.CompanySlug}/spaces/{space.Id}", new { space.Id, space.Name, space.Capacity, space.HourlyRate });
})
.RequireAuthorization();

// GET /{companySlug}/spaces/{id} - Get a specific space by ID (requires auth + membership)
tenantGroup.MapGet("/spaces/{id:guid}", (ApplicationDbContext db, ITenantContext tenantContext, ClaimsPrincipal user, Guid id) =>
{
    // Extract userId from "sub" claim
    var userIdClaim = user.FindFirst("sub")?.Value;
    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
    {
        return Results.Unauthorized();
    }

    var tenant = tenantContext.Current;
    if (tenant is null)
    {
        return Results.NotFound();
    }

    // Check membership
    var isMember = db.UserCompanyMemberships
        .AsNoTracking()
        .Any(m => m.UserId == userId && m.CompanyId == tenant.CompanyId);

    if (!isMember)
    {
        return Results.Forbid();
    }

    var space = db.Spaces
        .AsNoTracking()
        .Select(s => new { s.Id, s.Name, s.Capacity, s.HourlyRate, s.IsActive, s.CompanyId })
        .FirstOrDefault(s => s.Id == id && s.CompanyId == tenant.CompanyId);

    if (space is null)
    {
        return Results.NotFound();
    }

    return Results.Ok(space);
})
.RequireAuthorization();

// POST /{companySlug}/spaces/{id}/deactivate - Deactivate a space (requires auth + Manager/Admin/Owner)
tenantGroup.MapPost("/spaces/{id:guid}/deactivate", (ApplicationDbContext db, ITenantContext tenantContext, ClaimsPrincipal user, Guid id) =>
{
    // Extract userId from "sub" claim
    var userIdClaim = user.FindFirst("sub")?.Value;
    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
    {
        return Results.Unauthorized();
    }

    var tenant = tenantContext.Current;
    if (tenant is null)
    {
        return Results.NotFound();
    }

    // Query membership with role
    var membership = db.UserCompanyMemberships
        .AsNoTracking()
        .FirstOrDefault(m => m.UserId == userId && m.CompanyId == tenant.CompanyId);

    if (membership is null)
    {
        return Results.Forbid();
    }

    // Allow only: CompanyOwner, CompanyAdmin, CompanyManager
    var allowedRoles = new[] { TenantRoles.CompanyOwner, TenantRoles.CompanyAdmin, TenantRoles.CompanyManager };
    if (!allowedRoles.Contains(membership.Role))
    {
        return Results.Forbid();
    }

    var space = db.Spaces
        .FirstOrDefault(s => s.Id == id && s.CompanyId == tenant.CompanyId);

    if (space is null)
    {
        return Results.NotFound();
    }

    if (!space.IsActive)
    {
        return Results.Ok(new { id = space.Id, name = space.Name, isActive = space.IsActive });
    }

    space.Deactivate();
    db.SaveChanges();

    return Results.Ok(new { space.Id, space.Name, space.IsActive });
})
.RequireAuthorization();

// GET /{companySlug}/spaces/availability - Search available spaces for time range (requires auth + membership)
tenantGroup.MapGet("/spaces/availability", (ApplicationDbContext db, ITenantContext tenantContext, ClaimsPrincipal user, DateTime startUtc, DateTime endUtc, int? minCapacity, bool? onlyActive) =>
{
    // Extract userId from "sub" claim
    var userIdClaim = user.FindFirst("sub")?.Value;
    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
    {
        return Results.Unauthorized();
    }

    var tenant = tenantContext.Current;
    if (tenant is null)
    {
        return Results.NotFound();
    }

    // Check membership
    var isMember = db.UserCompanyMemberships
        .AsNoTracking()
        .Any(m => m.UserId == userId && m.CompanyId == tenant.CompanyId);

    if (!isMember)
    {
        return Results.Forbid();
    }

    // Validate time range
    if (startUtc >= endUtc)
    {
        return Results.BadRequest(new { error = "Start time must be before end time." });
    }

    // KISS safeguard: prevent absurdly large ranges (> 365 days)
    var maxRangeDays = 365;
    if ((endUtc - startUtc).TotalDays > maxRangeDays)
    {
        return Results.BadRequest(new { error = $"Time range cannot exceed {maxRangeDays} days." });
    }

    // Find "busy" space ids from overlapping non-cancelled bookings
    // Overlap rule: requestStartUtc < existingEndUtc && requestEndUtc > existingStartUtc
    var busySpaceIds = db.Bookings
        .AsNoTracking()
        .Where(b => !b.IsCancelled)
        .Where(b => b.StartUtc < endUtc && b.EndUtc > startUtc)
        .Join(db.BookingSpaces.AsNoTracking(), b => b.Id, bs => bs.BookingId, (b, bs) => bs.SpaceId)
        .Distinct()
        .ToList();

    // Build base query for spaces
    var query = db.Spaces.AsNoTracking();

    // Apply onlyActive filter (default true)
    var activeFilter = onlyActive ?? true;
    if (activeFilter)
    {
        query = query.Where(s => s.IsActive);
    }

    // Apply minCapacity filter if provided
    if (minCapacity.HasValue && minCapacity.Value > 0)
    {
        query = query.Where(s => s.Capacity >= minCapacity.Value);
    }

    // Return spaces NOT in busy list
    var availableSpaces = query
        .Where(s => !busySpaceIds.Contains(s.Id))
        .OrderBy(s => s.Name)
        .Select(s => new AvailableSpaceResponse(s.Id, s.Name, s.Capacity))
        .ToList();

    return Results.Ok(availableSpaces);
})
.RequireAuthorization();

// SpaceConfiguration endpoints (foundation for combinable rooms)

// GET /{companySlug}/space-configurations - Lists space configurations for current tenant (requires auth + membership)
tenantGroup.MapGet("/space-configurations", (ApplicationDbContext db, ITenantContext tenantContext, ClaimsPrincipal user) =>
{
    // Extract userId from "sub" claim
    var userIdClaim = user.FindFirst("sub")?.Value;
    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
    {
        return Results.Unauthorized();
    }

    var tenant = tenantContext.Current;
    if (tenant is null)
    {
        return Results.NotFound();
    }

    // Check membership
    var isMember = db.UserCompanyMemberships
        .AsNoTracking()
        .Any(m => m.UserId == userId && m.CompanyId == tenant.CompanyId);

    if (!isMember)
    {
        return Results.Forbid();
    }

    var configs = db.SpaceConfigurations
        .AsNoTracking()
        .OrderBy(sc => sc.Name)
        .Select(sc => new { sc.Id, sc.Name, sc.HourlyRateOverride, sc.MinBookingMinutesOverride, sc.IsActive, sc.CompanyId })
        .ToList();

    return Results.Ok(configs);
})
.RequireAuthorization();

// POST /{companySlug}/space-configurations - Creates a new space configuration for current tenant (requires auth + Manager/Admin/Owner)
tenantGroup.MapPost("/space-configurations", (ApplicationDbContext db, ITenantContext tenantContext, ClaimsPrincipal user, CreateSpaceConfigurationRequest request) =>
{
    // Extract userId from "sub" claim
    var userIdClaim = user.FindFirst("sub")?.Value;
    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
    {
        return Results.Unauthorized();
    }

    var tenant = tenantContext.Current;
    if (tenant is null)
    {
        return Results.NotFound();
    }

    // Query membership with role
    var membership = db.UserCompanyMemberships
        .AsNoTracking()
        .FirstOrDefault(m => m.UserId == userId && m.CompanyId == tenant.CompanyId);

    if (membership is null)
    {
        return Results.Forbid();
    }

    // Allow only: CompanyOwner, CompanyAdmin, CompanyManager
    var allowedRoles = new[] { TenantRoles.CompanyOwner, TenantRoles.CompanyAdmin, TenantRoles.CompanyManager };
    if (!allowedRoles.Contains(membership.Role))
    {
        return Results.Forbid();
    }

    // Validate request
    if (string.IsNullOrWhiteSpace(request.Name))
    {
        return Results.BadRequest(new { error = "Name is required." });
    }
    if (request.HourlyRateOverride.HasValue && request.HourlyRateOverride.Value < 0)
    {
        return Results.BadRequest(new { error = "Hourly rate override cannot be negative." });
    }
    if (request.MinBookingMinutesOverride.HasValue && request.MinBookingMinutesOverride.Value <= 0)
    {
        return Results.BadRequest(new { error = "Minimum booking minutes override must be greater than 0." });
    }

    var config = new SpaceConfiguration(tenant.CompanyId, request.Name.Trim(), request.HourlyRateOverride, request.MinBookingMinutesOverride);
    db.SpaceConfigurations.Add(config);
    db.SaveChanges();

    return Results.Created($"/{tenant.CompanySlug}/space-configurations/{config.Id}", new { config.Id, config.Name, config.HourlyRateOverride, config.MinBookingMinutesOverride });
})
.RequireAuthorization();

// GET /{companySlug}/space-configurations/{id} - Get a specific space configuration with included space IDs (requires auth + membership)
tenantGroup.MapGet("/space-configurations/{id:guid}", (ApplicationDbContext db, ITenantContext tenantContext, ClaimsPrincipal user, Guid id) =>
{
    // Extract userId from "sub" claim
    var userIdClaim = user.FindFirst("sub")?.Value;
    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
    {
        return Results.Unauthorized();
    }

    var tenant = tenantContext.Current;
    if (tenant is null)
    {
        return Results.NotFound();
    }

    // Check membership
    var isMember = db.UserCompanyMemberships
        .AsNoTracking()
        .Any(m => m.UserId == userId && m.CompanyId == tenant.CompanyId);

    if (!isMember)
    {
        return Results.Forbid();
    }

    var config = db.SpaceConfigurations
        .AsNoTracking()
        .Select(sc => new { sc.Id, sc.Name, sc.HourlyRateOverride, sc.MinBookingMinutesOverride, sc.IsActive, sc.CompanyId })
        .FirstOrDefault(sc => sc.Id == id && sc.CompanyId == tenant.CompanyId);

    if (config is null)
    {
        return Results.NotFound();
    }

    // Get associated space IDs
    var spaceIds = db.SpaceConfigurationSpaces
        .AsNoTracking()
        .Where(scs => scs.SpaceConfigurationId == id)
        .Select(scs => scs.SpaceId)
        .ToList();

    // Get space names for convenience
    var spaces = db.Spaces
        .AsNoTracking()
        .Where(s => spaceIds.Contains(s.Id))
        .Select(s => new { s.Id, s.Name })
        .ToList();

    return Results.Ok(new
    {
        config.Id,
        config.Name,
        config.HourlyRateOverride,
        config.MinBookingMinutesOverride,
        config.IsActive,
        config.CompanyId,
        SpaceIds = spaceIds,
        Spaces = spaces
    });
})
.RequireAuthorization();

// PUT /{companySlug}/space-configurations/{id}/spaces - Replace included spaces for a configuration (requires auth + Manager/Admin/Owner)
tenantGroup.MapPut("/space-configurations/{id:guid}/spaces", async (ApplicationDbContext db, ITenantContext tenantContext, ClaimsPrincipal user, Guid id, SetSpaceConfigurationSpacesRequest request) =>
{
    // Extract userId from "sub" claim
    var userIdClaim = user.FindFirst("sub")?.Value;
    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
    {
        return Results.Unauthorized();
    }

    var tenant = tenantContext.Current;
    if (tenant is null)
    {
        return Results.NotFound();
    }

    // Query membership with role
    var membership = db.UserCompanyMemberships
        .AsNoTracking()
        .FirstOrDefault(m => m.UserId == userId && m.CompanyId == tenant.CompanyId);

    if (membership is null)
    {
        return Results.Forbid();
    }

    // Allow only: CompanyOwner, CompanyAdmin, CompanyManager
    var allowedRoles = new[] { TenantRoles.CompanyOwner, TenantRoles.CompanyAdmin, TenantRoles.CompanyManager };
    if (!allowedRoles.Contains(membership.Role))
    {
        return Results.Forbid();
    }

    // Verify the space configuration exists and belongs to this tenant
    var config = db.SpaceConfigurations
        .FirstOrDefault(sc => sc.Id == id && sc.CompanyId == tenant.CompanyId);

    if (config is null)
    {
        return Results.NotFound();
    }

    // Validate that all space IDs exist and belong to this tenant
    if (request.SpaceIds.Count > 0)
    {
        var requestedSpaceIds = request.SpaceIds.ToHashSet();
        var existingSpaces = db.Spaces
            .AsNoTracking()
            .Where(s => requestedSpaceIds.Contains(s.Id) && s.CompanyId == tenant.CompanyId)
            .Select(s => s.Id)
            .ToList();

        if (existingSpaces.Count != requestedSpaceIds.Count)
        {
            var missingIds = requestedSpaceIds.Except(existingSpaces);
            return Results.BadRequest(new { error = "One or more space IDs are invalid or do not belong to this tenant.", missingIds });
        }
    }

    // Remove existing associations
    var existingAssociations = db.SpaceConfigurationSpaces
        .Where(scs => scs.SpaceConfigurationId == id)
        .ToList();

    db.SpaceConfigurationSpaces.RemoveRange(existingAssociations);

    // Add new associations
    foreach (var spaceId in request.SpaceIds)
    {
        db.SpaceConfigurationSpaces.Add(new SpaceConfigurationSpace
        {
            SpaceConfigurationId = id,
            SpaceId = spaceId
        });
    }

    await db.SaveChangesAsync();

    // Return the updated space IDs
    var updatedSpaceIds = db.SpaceConfigurationSpaces
        .AsNoTracking()
        .Where(scs => scs.SpaceConfigurationId == id)
        .Select(scs => scs.SpaceId)
        .ToList();

    return Results.Ok(new
    {
        config.Id,
        config.Name,
        SpaceIds = updatedSpaceIds
    });
})
.RequireAuthorization();

// Booking endpoints

// GET /{companySlug}/bookings - Lists bookings for current tenant (requires auth + any membership)
tenantGroup.MapGet("/bookings", (ApplicationDbContext db, ITenantContext tenantContext, ClaimsPrincipal user, DateTime? startUtc, DateTime? endUtc, Guid? clientId, bool? includeCancelled) =>
{
    // Extract userId from "sub" claim
    var userIdClaim = user.FindFirst("sub")?.Value;
    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
    {
        return Results.Unauthorized();
    }

    var tenant = tenantContext.Current;
    if (tenant is null)
    {
        return Results.NotFound();
    }

    // Check membership
    var isMember = db.UserCompanyMemberships
        .AsNoTracking()
        .Any(m => m.UserId == userId && m.CompanyId == tenant.CompanyId);

    if (!isMember)
    {
        return Results.Forbid();
    }

    // Validate date range
    if (startUtc.HasValue && endUtc.HasValue && startUtc.Value >= endUtc.Value)
    {
        return Results.BadRequest(new { error = "Start time must be before end time." });
    }

    // KISS safeguard: prevent absurdly large ranges (> 365 days)
    if (startUtc.HasValue && endUtc.HasValue)
    {
        var maxRangeDays = 365;
        if ((endUtc.Value - startUtc.Value).TotalDays > maxRangeDays)
        {
            return Results.BadRequest(new { error = $"Time range cannot exceed {maxRangeDays} days." });
        }
    }

    // Build query with filters
    var query = db.Bookings.AsNoTracking();

    // Date range filtering (overlap detection)
    if (startUtc.HasValue && endUtc.HasValue)
    {
        // Both provided: return bookings overlapping the range
        query = query.Where(b => startUtc.Value < b.EndUtc && endUtc.Value > b.StartUtc);
    }
    else if (startUtc.HasValue)
    {
        // Only start provided: bookings ending after start
        query = query.Where(b => b.EndUtc > startUtc.Value);
    }
    else if (endUtc.HasValue)
    {
        // Only end provided: bookings starting before end
        query = query.Where(b => b.StartUtc < endUtc.Value);
    }

    // Client filter
    if (clientId.HasValue)
    {
        query = query.Where(b => b.ClientId == clientId.Value);
    }

    // Status filter (default: exclude cancelled)
    var includeCancelledBookings = includeCancelled ?? false;
    if (!includeCancelledBookings)
    {
        query = query.Where(b => !b.IsCancelled);
    }

    // Sort: StartUtc ascending, then Title
    var bookings = query
        .OrderBy(b => b.StartUtc)
        .ThenBy(b => b.Title)
        .Select(b => new
        {
            b.Id,
            b.ClientId,
            b.Title,
            b.StartUtc,
            b.EndUtc,
            b.AttendeeCount,
            b.IsCancelled,
            b.TotalAmount,
            b.SpaceConfigurationId,
            b.Status,
            b.CancelledUtc,
            b.CancelReason
        })
        .ToList();

    // Get space IDs for all bookings in one query
    var bookingIds = bookings.Select(b => b.Id).ToList();
    var bookingSpaces = db.BookingSpaces
        .AsNoTracking()
        .Where(bs => bookingIds.Contains(bs.BookingId))
        .ToList();

    var spaceIdsByBooking = bookingSpaces
        .GroupBy(bs => bs.BookingId)
        .ToDictionary(g => g.Key, g => g.Select(bs => bs.SpaceId).ToList());

    var response = bookings.Select(b => new BookingResponse(
        b.Id,
        b.ClientId,
        b.Title,
        b.StartUtc,
        b.EndUtc,
        b.AttendeeCount,
        b.IsCancelled,
        spaceIdsByBooking.TryGetValue(b.Id, out var spaceIds) ? spaceIds : new List<Guid>(),
        b.TotalAmount,
        b.SpaceConfigurationId,
        b.Status,
        b.CancelledUtc,
        b.CancelReason));

    return Results.Ok(response);
})
.RequireAuthorization();

// POST /{companySlug}/bookings - Creates a new booking for current tenant (requires auth + Manager/Admin/Owner)
tenantGroup.MapPost("/bookings", async (ApplicationDbContext db, ITenantContext tenantContext, ClaimsPrincipal user, CreateBookingRequest request) =>
{
    // Extract userId from "sub" claim
    var userIdClaim = user.FindFirst("sub")?.Value;
    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
    {
        return Results.Unauthorized();
    }

    var tenant = tenantContext.Current;
    if (tenant is null)
    {
        return Results.NotFound();
    }

    // Query membership with role
    var membership = db.UserCompanyMemberships
        .AsNoTracking()
        .FirstOrDefault(m => m.UserId == userId && m.CompanyId == tenant.CompanyId);

    if (membership is null)
    {
        return Results.Forbid();
    }

    // Allow only: CompanyOwner, CompanyAdmin, CompanyManager
    var allowedRoles = new[] { TenantRoles.CompanyOwner, TenantRoles.CompanyAdmin, TenantRoles.CompanyManager };
    if (!allowedRoles.Contains(membership.Role))
    {
        return Results.Forbid();
    }

    // Validate request
    if (string.IsNullOrWhiteSpace(request.Title))
    {
        return Results.BadRequest(new { error = "Title is required." });
    }
    if (request.Title.Length > 200)
    {
        return Results.BadRequest(new { error = "Title cannot exceed 200 characters." });
    }
    if (request.StartUtc >= request.EndUtc)
    {
        return Results.BadRequest(new { error = "Start time must be before end time." });
    }
    if (request.AttendeeCount < 0)
    {
        return Results.BadRequest(new { error = "Attendee count cannot be negative." });
    }

    // Verify client exists and belongs to this tenant
    var clientExists = db.Clients
        .AsNoTracking()
        .Any(c => c.Id == request.ClientId && c.CompanyId == tenant.CompanyId);

    if (!clientExists)
    {
        return Results.BadRequest(new { error = "Client not found or does not belong to this tenant." });
    }

    // Validate SpaceConfigurationId if provided
    if (request.SpaceConfigurationId.HasValue)
    {
        var configExists = db.SpaceConfigurations
            .AsNoTracking()
            .Any(sc => sc.Id == request.SpaceConfigurationId.Value && sc.CompanyId == tenant.CompanyId && sc.IsActive);

        if (!configExists)
        {
            return Results.BadRequest(new { error = "Space configuration not found, does not belong to this tenant, or is not active." });
        }
    }

    // Validate minimum booking duration if SpaceConfiguration has override
    var (durationOk, minMinutes) = await ValidateMinBookingDurationAsync(
        request.SpaceConfigurationId,
        request.StartUtc,
        request.EndUtc,
        db);

    if (!durationOk)
    {
        return Results.BadRequest(new {
            error = "Booking duration is below the minimum allowed.",
            minMinutes = minMinutes
        });
    }

    var booking = new Booking(tenant.CompanyId, request.ClientId, request.Title, request.StartUtc, request.EndUtc, request.AttendeeCount);

    // Set SpaceConfigurationId if provided via type-safe setter
    if (request.SpaceConfigurationId.HasValue)
    {
        booking.SetSpaceConfigurationId(request.SpaceConfigurationId.Value);
    }
    
    db.Bookings.Add(booking);
    db.SaveChanges();

    return Results.Created(
        $"/{tenant.CompanySlug}/bookings/{booking.Id}",
        new BookingResponse(booking.Id, booking.ClientId, booking.Title, booking.StartUtc, booking.EndUtc, booking.AttendeeCount, booking.IsCancelled, new List<Guid>(), booking.TotalAmount, booking.SpaceConfigurationId, booking.Status, booking.CancelledUtc, booking.CancelReason));
})
.RequireAuthorization();

// POST /{companySlug}/bookings/with-spaces - Creates a new booking with spaces atomically (requires auth + Manager/Admin/Owner)
tenantGroup.MapPost("/bookings/with-spaces", async (ApplicationDbContext db, ITenantContext tenantContext, ClaimsPrincipal user, CreateBookingWithSpacesRequest request) =>
{
    // Extract userId from "sub" claim
    var userIdClaim = user.FindFirst("sub")?.Value;
    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
    {
        return Results.Unauthorized();
    }

    var tenant = tenantContext.Current;
    if (tenant is null)
    {
        return Results.NotFound();
    }

    // Query membership with role
    var membership = db.UserCompanyMemberships
        .AsNoTracking()
        .FirstOrDefault(m => m.UserId == userId && m.CompanyId == tenant.CompanyId);

    if (membership is null)
    {
        return Results.Forbid();
    }

    // Allow only: CompanyOwner, CompanyAdmin, CompanyManager
    var allowedRoles = new[] { TenantRoles.CompanyOwner, TenantRoles.CompanyAdmin, TenantRoles.CompanyManager };
    if (!allowedRoles.Contains(membership.Role))
    {
        return Results.Forbid();
    }

    // 1) Validate basic fields
    if (string.IsNullOrWhiteSpace(request.Title))
    {
        return Results.BadRequest(new { error = "Title is required." });
    }
    if (request.Title.Length > 200)
    {
        return Results.BadRequest(new { error = "Title cannot exceed 200 characters." });
    }
    if (request.StartUtc >= request.EndUtc)
    {
        return Results.BadRequest(new { error = "Start time must be before end time." });
    }
    if (request.AttendeeCount < 0)
    {
        return Results.BadRequest(new { error = "Attendee count cannot be negative." });
    }

    // 2) Validate Client exists in tenant
    var clientExists = db.Clients
        .AsNoTracking()
        .Any(c => c.Id == request.ClientId && c.CompanyId == tenant.CompanyId);

    if (!clientExists)
    {
        return Results.BadRequest(new { error = "Client not found or does not belong to this tenant." });
    }

    // 3) Validate SpaceConfigurationId if provided
    if (request.SpaceConfigurationId.HasValue)
    {
        var configExists = db.SpaceConfigurations
            .AsNoTracking()
            .Any(sc => sc.Id == request.SpaceConfigurationId.Value && sc.CompanyId == tenant.CompanyId && sc.IsActive);

        if (!configExists)
        {
            return Results.BadRequest(new { error = "Space configuration not found, does not belong to this tenant, or is not active." });
        }
    }

    // 4) Validate minimum booking duration
    var (durationOk, minMinutes) = await ValidateMinBookingDurationAsync(
        request.SpaceConfigurationId,
        request.StartUtc,
        request.EndUtc,
        db);

    if (!durationOk)
    {
        return Results.BadRequest(new {
            error = "Booking duration is below the minimum allowed.",
            minMinutes = minMinutes
        });
    }

    // 5) Validate SpaceIds
    if (request.SpaceIds.Count == 0)
    {
        return Results.BadRequest(new { error = "SpaceIds is required and must not be empty." });
    }

    // Deduplicate SpaceIds
    var dedupedSpaceIds = request.SpaceIds.ToHashSet();

    // Ensure all spaces exist in tenant
    var existingSpaceIds = db.Spaces
        .AsNoTracking()
        .Where(s => dedupedSpaceIds.Contains(s.Id) && s.CompanyId == tenant.CompanyId)
        .Select(s => s.Id)
        .ToList();

    if (existingSpaceIds.Count != dedupedSpaceIds.Count)
    {
        var missingIds = dedupedSpaceIds.Except(existingSpaceIds).ToList();
        return Results.BadRequest(new { error = "One or more space IDs are invalid or do not belong to this tenant.", missingSpaceIds = missingIds });
    }

    // 6) Conflict detection
    var (conflictingBookingIds, conflictingSpaceIds) = FindConflictingBookings(
        db,
        tenant.CompanyId,
        request.StartUtc,
        request.EndUtc,
        dedupedSpaceIds,
        null); // No booking to exclude for create

    if (conflictingBookingIds.Count > 0)
    {
        return Results.Conflict(new BookingConflictResponse(
            "Booking conflicts with existing bookings.",
            conflictingBookingIds,
            conflictingSpaceIds));
    }

    // 7) Create Booking
    var booking = new Booking(tenant.CompanyId, request.ClientId, request.Title, request.StartUtc, request.EndUtc, request.AttendeeCount);

    // Set SpaceConfigurationId if provided
    if (request.SpaceConfigurationId.HasValue)
    {
        booking.SetSpaceConfigurationId(request.SpaceConfigurationId.Value);
    }

    db.Bookings.Add(booking);

    // 8) Create BookingSpaces rows
    foreach (var spaceId in dedupedSpaceIds)
    {
        db.BookingSpaces.Add(new BookingSpace
        {
            BookingId = booking.Id,
            SpaceId = spaceId
        });
    }

    // 9) Calculate TotalAmount
    // Load space hourly rates
    var spaceHourlyRates = db.Spaces
        .AsNoTracking()
        .Where(s => dedupedSpaceIds.Contains(s.Id))
        .Select(s => s.HourlyRate)
        .ToList();

    // Load space configuration override rate if set
    decimal? overrideRate = null;
    if (request.SpaceConfigurationId.HasValue)
    {
        overrideRate = db.SpaceConfigurations
            .AsNoTracking()
            .Where(sc => sc.Id == request.SpaceConfigurationId.Value)
            .Select(sc => sc.HourlyRateOverride)
            .FirstOrDefault();
    }

    var totalAmount = CalculateBookingTotal(request.StartUtc, request.EndUtc, spaceHourlyRates, overrideRate);
    booking.SetTotalAmount(totalAmount);

    // 10) SaveChanges (atomic)
    await db.SaveChangesAsync();

    // 11) Return 201 Created with BookingResponse
    return Results.Created(
        $"/{tenant.CompanySlug}/bookings/{booking.Id}",
        new BookingResponse(
            booking.Id,
            booking.ClientId,
            booking.Title,
            booking.StartUtc,
            booking.EndUtc,
            booking.AttendeeCount,
            booking.IsCancelled,
            dedupedSpaceIds.ToList(),
            booking.TotalAmount,
            booking.SpaceConfigurationId,
            booking.Status,
            booking.CancelledUtc,
            booking.CancelReason));
})
.RequireAuthorization();

// DELETE /{companySlug}/bookings/{id} - Soft cancel a booking (requires auth + Manager/Admin/Owner)
tenantGroup.MapDelete("/bookings/{id:guid}", (ApplicationDbContext db, ITenantContext tenantContext, ClaimsPrincipal user, Guid id, string? reason = null) =>
{
    // Extract userId from "sub" claim
    var userIdClaim = user.FindFirst("sub")?.Value;
    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
    {
        return Results.Unauthorized();
    }

    var tenant = tenantContext.Current;
    if (tenant is null)
    {
        return Results.NotFound();
    }

    // Query membership with role
    var membership = db.UserCompanyMemberships
        .AsNoTracking()
        .FirstOrDefault(m => m.UserId == userId && m.CompanyId == tenant.CompanyId);

    if (membership is null)
    {
        return Results.Forbid();
    }

    // Allow only: CompanyOwner, CompanyAdmin, CompanyManager
    var allowedRoles = new[] { TenantRoles.CompanyOwner, TenantRoles.CompanyAdmin, TenantRoles.CompanyManager };
    if (!allowedRoles.Contains(membership.Role))
    {
        return Results.Forbid();
    }

    var booking = db.Bookings
        .FirstOrDefault(b => b.Id == id && b.CompanyId == tenant.CompanyId);

    if (booking is null)
    {
        return Results.NotFound();
    }

    // Idempotent: if already cancelled, just return 204 (do not overwrite existing metadata)
    if (booking.IsCancelled)
    {
        return Results.NoContent();
    }

    booking.Cancel(reason, DateTime.UtcNow);
    db.SaveChanges();

    return Results.NoContent();
})
.RequireAuthorization();

// POST /{companySlug}/bookings/{id}/confirm - Confirm a booking (requires auth + Manager/Admin/Owner)
tenantGroup.MapPost("/bookings/{id:guid}/confirm", (ApplicationDbContext db, ITenantContext tenantContext, ClaimsPrincipal user, Guid id) =>
{
    // Extract userId from "sub" claim
    var userIdClaim = user.FindFirst("sub")?.Value;
    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
    {
        return Results.Unauthorized();
    }

    var tenant = tenantContext.Current;
    if (tenant is null)
    {
        return Results.NotFound();
    }

    // Query membership with role
    var membership = db.UserCompanyMemberships
        .AsNoTracking()
        .FirstOrDefault(m => m.UserId == userId && m.CompanyId == tenant.CompanyId);

    if (membership is null)
    {
        return Results.Forbid();
    }

    // Allow only: CompanyOwner, CompanyAdmin, CompanyManager
    var allowedRoles = new[] { TenantRoles.CompanyOwner, TenantRoles.CompanyAdmin, TenantRoles.CompanyManager };
    if (!allowedRoles.Contains(membership.Role))
    {
        return Results.Forbid();
    }

    var booking = db.Bookings
        .FirstOrDefault(b => b.Id == id && b.CompanyId == tenant.CompanyId);

    if (booking is null)
    {
        return Results.NotFound();
    }

    // Cannot confirm cancelled bookings
    if (booking.IsCancelled)
    {
        return Results.BadRequest(new { error = "Cancelled booking cannot be confirmed." });
    }

    // Idempotent: if already confirmed, return 204
    if (booking.Status == BookingStatus.Confirmed)
    {
        return Results.NoContent();
    }

    booking.Confirm();
    db.SaveChanges();

    return Results.NoContent();
})
.RequireAuthorization();

// GET /{companySlug}/bookings/{id} - Get a specific booking with space IDs (requires auth + membership)
tenantGroup.MapGet("/bookings/{id:guid}", (ApplicationDbContext db, ITenantContext tenantContext, ClaimsPrincipal user, Guid id) =>
{
    // Extract userId from "sub" claim
    var userIdClaim = user.FindFirst("sub")?.Value;
    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
    {
        return Results.Unauthorized();
    }

    var tenant = tenantContext.Current;
    if (tenant is null)
    {
        return Results.NotFound();
    }

    // Check membership
    var isMember = db.UserCompanyMemberships
        .AsNoTracking()
        .Any(m => m.UserId == userId && m.CompanyId == tenant.CompanyId);

    if (!isMember)
    {
        return Results.Forbid();
    }

    var booking = db.Bookings
        .AsNoTracking()
        .Select(b => new { b.Id, b.ClientId, b.Title, b.StartUtc, b.EndUtc, b.AttendeeCount, b.IsCancelled, b.CompanyId, b.TotalAmount, b.SpaceConfigurationId, b.Status, b.CancelledUtc, b.CancelReason })
        .FirstOrDefault(b => b.Id == id && b.CompanyId == tenant.CompanyId);

    if (booking is null)
    {
        return Results.NotFound();
    }

    var spaceIds = db.BookingSpaces
        .AsNoTracking()
        .Where(bs => bs.BookingId == id)
        .Select(bs => bs.SpaceId)
        .ToList();

    return Results.Ok(new BookingResponse(
        booking.Id,
        booking.ClientId,
        booking.Title,
        booking.StartUtc,
        booking.EndUtc,
        booking.AttendeeCount,
        booking.IsCancelled,
        spaceIds,
        booking.TotalAmount,
        booking.SpaceConfigurationId,
        booking.Status,
        booking.CancelledUtc,
        booking.CancelReason));
})
.RequireAuthorization();

// GET /{companySlug}/bookings/{id}/details - Get detailed booking info including client name and space names (requires auth + membership)
tenantGroup.MapGet("/bookings/{id:guid}/details", (ApplicationDbContext db, ITenantContext tenantContext, ClaimsPrincipal user, Guid id) =>
{
    // Extract userId from "sub" claim
    var userIdClaim = user.FindFirst("sub")?.Value;
    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
    {
        return Results.Unauthorized();
    }

    var tenant = tenantContext.Current;
    if (tenant is null)
    {
        return Results.NotFound();
    }

    // Check membership
    var isMember = db.UserCompanyMemberships
        .AsNoTracking()
        .Any(m => m.UserId == userId && m.CompanyId == tenant.CompanyId);

    if (!isMember)
    {
        return Results.Forbid();
    }

    // Load booking (tenant-filtered)
    var booking = db.Bookings
        .AsNoTracking()
        .Select(b => new { b.Id, b.ClientId, b.Title, b.StartUtc, b.EndUtc, b.AttendeeCount, b.IsCancelled, b.CompanyId, b.TotalAmount, b.SpaceConfigurationId, b.Status, b.CancelledUtc, b.CancelReason })
        .FirstOrDefault(b => b.Id == id && b.CompanyId == tenant.CompanyId);

    if (booking is null)
    {
        return Results.NotFound();
    }

    // Load client name (tenant-filtered)
    var clientName = db.Clients
        .AsNoTracking()
        .Where(c => c.Id == booking.ClientId && c.CompanyId == tenant.CompanyId)
        .Select(c => c.Name)
        .FirstOrDefault();

    if (clientName is null)
    {
        // This shouldn't happen due to FK constraints, but handle gracefully
        return Results.Problem("Booking client not found.", statusCode: 500);
    }

    // Load attached spaces (Ids + Names) via BookingSpaces join, ordered by Name
    var spaces = db.BookingSpaces
        .AsNoTracking()
        .Where(bs => bs.BookingId == id)
        .Join(
            db.Spaces.AsNoTracking().Where(s => s.CompanyId == tenant.CompanyId),
            bs => bs.SpaceId,
            s => s.Id,
            (bs, s) => new SpaceSummary(s.Id, s.Name))
        .OrderBy(s => s.Name)
        .ToList();

    var response = new BookingDetailsResponse(
        booking.Id,
        booking.ClientId,
        clientName,
        booking.Title,
        booking.StartUtc,
        booking.EndUtc,
        booking.AttendeeCount,
        booking.IsCancelled,
        booking.TotalAmount,
        booking.SpaceConfigurationId,
        spaces,
        booking.Status,
        booking.CancelledUtc,
        booking.CancelReason);

    return Results.Ok(response);
})
.RequireAuthorization();

// Local helper: Find conflicting bookings for space-level conflict detection
// Returns conflicting booking IDs and space IDs
static (IReadOnlyList<Guid> BookingIds, IReadOnlyList<Guid> SpaceIds) FindConflictingBookings(
    ApplicationDbContext db,
    Guid companyId,
    DateTime startUtc,
    DateTime endUtc,
    IEnumerable<Guid> targetSpaceIds,
    Guid? excludeBookingId)
{
    var spaceIdSet = targetSpaceIds.ToHashSet();
    if (spaceIdSet.Count == 0)
    {
        return (Array.Empty<Guid>(), Array.Empty<Guid>());
    }

    // Query: overlapping bookings sharing any of the target spaces
    // Overlap rule: newStart < existingEnd && newEnd > existingStart
    var query = db.Bookings
        .AsNoTracking()
        .Where(b => b.CompanyId == companyId)
        .Where(b => !b.IsCancelled)
        .Where(b => b.StartUtc < endUtc && b.EndUtc > startUtc)
        .Where(b => db.BookingSpaces.AsNoTracking().Any(bs => bs.BookingId == b.Id && spaceIdSet.Contains(bs.SpaceId)));

    if (excludeBookingId.HasValue)
    {
        query = query.Where(b => b.Id != excludeBookingId.Value);
    }

    var conflictingBookings = query
        .Select(b => new { b.Id })
        .ToList();

    if (conflictingBookings.Count == 0)
    {
        return (Array.Empty<Guid>(), Array.Empty<Guid>());
    }

    var conflictingBookingIds = conflictingBookings.Select(b => b.Id).ToList();

    // Find which specific spaces conflict
    var conflictingSpaceIds = db.BookingSpaces
        .AsNoTracking()
        .Where(bs => conflictingBookingIds.Contains(bs.BookingId) && spaceIdSet.Contains(bs.SpaceId))
        .Select(bs => bs.SpaceId)
        .Distinct()
        .ToList();

    return (conflictingBookingIds, conflictingSpaceIds);
}

// Local helper: Validate minimum booking duration against SpaceConfiguration override
// Returns (ok, minMinutes) - if ok is false, minMinutes contains the required minimum
static async Task<(bool ok, int? minMinutes)> ValidateMinBookingDurationAsync(
    Guid? spaceConfigurationId,
    DateTime startUtc,
    DateTime endUtc,
    ApplicationDbContext db)
{
    // If no SpaceConfiguration selected, no minimum duration rule applies
    if (spaceConfigurationId == null)
    {
        return (true, null);
    }

    // Load SpaceConfiguration using existing tenant filtering
    var config = await db.SpaceConfigurations
        .AsNoTracking()
        .FirstOrDefaultAsync(sc => sc.Id == spaceConfigurationId.Value);

    // If config not found or override not set, no validation
    if (config?.MinBookingMinutesOverride == null || config.MinBookingMinutesOverride <= 0)
    {
        return (true, null);
    }

    var minMinutes = config.MinBookingMinutesOverride.Value;
    var durationMinutes = (endUtc - startUtc).TotalMinutes;

    // Validate actual duration (not billing duration)
    if (durationMinutes < minMinutes)
    {
        return (false, minMinutes);
    }

    return (true, null);
}

// Local helper: Calculate booking total amount based on duration and space rates
// - Minutes rounded UP to nearest 15 (ceiling)
// - Convert to hours as decimal
// - If overrideRate is set, use that for ALL spaces
// - Otherwise, use each space's hourly rate
// - Round to 2 decimals using MidpointRounding.AwayFromZero
static decimal CalculateBookingTotal(
    DateTime startUtc,
    DateTime endUtc,
    IReadOnlyList<decimal> spaceHourlyRates,
    decimal? bookingWideOverrideHourlyRate = null)
{
    var durationMinutes = (endUtc - startUtc).TotalMinutes;
    if (durationMinutes <= 0 || spaceHourlyRates.Count == 0)
        return 0;

    // Round UP to nearest 15 minutes
    var billingUnits = (int)Math.Ceiling(durationMinutes / 15.0);
    var billedMinutes = billingUnits * 15;
    var billedHours = billedMinutes / 60m;

    decimal total;

    // If override rate is provided, use it for all spaces
    if (bookingWideOverrideHourlyRate.HasValue)
    {
        total = bookingWideOverrideHourlyRate.Value * billedHours * spaceHourlyRates.Count;
    }
    else
    {
        // Sum rates * hours per space
        total = spaceHourlyRates.Sum(rate => rate * billedHours);
    }

    // Round to 2 decimals
    return Math.Round(total, 2, MidpointRounding.AwayFromZero);
}

// PUT /{companySlug}/bookings/{id} - Update booking details (requires auth + Manager/Admin/Owner)
tenantGroup.MapPut("/bookings/{id:guid}", async (ApplicationDbContext db, ITenantContext tenantContext, ClaimsPrincipal user, Guid id, UpdateBookingRequest request) =>
{
    // Extract userId from "sub" claim
    var userIdClaim = user.FindFirst("sub")?.Value;
    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
    {
        return Results.Unauthorized();
    }

    var tenant = tenantContext.Current;
    if (tenant is null)
    {
        return Results.NotFound();
    }

    // Query membership with role
    var membership = db.UserCompanyMemberships
        .AsNoTracking()
        .FirstOrDefault(m => m.UserId == userId && m.CompanyId == tenant.CompanyId);

    if (membership is null)
    {
        return Results.Forbid();
    }

    // Allow only: CompanyOwner, CompanyAdmin, CompanyManager
    var allowedRoles = new[] { TenantRoles.CompanyOwner, TenantRoles.CompanyAdmin, TenantRoles.CompanyManager };
    if (!allowedRoles.Contains(membership.Role))
    {
        return Results.Forbid();
    }

    // Load booking
    var booking = db.Bookings
        .FirstOrDefault(b => b.Id == id && b.CompanyId == tenant.CompanyId);

    if (booking is null)
    {
        return Results.NotFound();
    }

    // Reject updates to cancelled bookings
    if (booking.IsCancelled)
    {
        return Results.BadRequest(new { error = "Cancelled booking cannot be modified." });
    }

    // Reject updates to confirmed bookings
    if (booking.Status == BookingStatus.Confirmed)
    {
        return Results.BadRequest(new { error = "Confirmed booking cannot be modified." });
    }

    // Validate request (same as create)
    if (string.IsNullOrWhiteSpace(request.Title))
    {
        return Results.BadRequest(new { error = "Title is required." });
    }
    if (request.Title.Length > 200)
    {
        return Results.BadRequest(new { error = "Title cannot exceed 200 characters." });
    }
    if (request.StartUtc >= request.EndUtc)
    {
        return Results.BadRequest(new { error = "Start time must be before end time." });
    }
    if (request.AttendeeCount < 0)
    {
        return Results.BadRequest(new { error = "Attendee count cannot be negative." });
    }

    // Validate SpaceConfigurationId if provided
    if (request.SpaceConfigurationId.HasValue)
    {
        var configExists = db.SpaceConfigurations
            .AsNoTracking()
            .Any(sc => sc.Id == request.SpaceConfigurationId.Value && sc.CompanyId == tenant.CompanyId && sc.IsActive);

        if (!configExists)
        {
            return Results.BadRequest(new { error = "Space configuration not found, does not belong to this tenant, or is not active." });
        }
    }

    // Validate minimum booking duration if SpaceConfiguration has override
    // Use the request's SpaceConfigurationId (may be changing) and request's time values
    var (durationOk, minMinutes) = await ValidateMinBookingDurationAsync(
        request.SpaceConfigurationId,
        request.StartUtc,
        request.EndUtc,
        db);

    if (!durationOk)
    {
        return Results.BadRequest(new {
            error = "Booking duration is below the minimum allowed.",
            minMinutes = minMinutes
        });
    }

    // Get existing spaces attached to this booking
    var existingSpaceIds = db.BookingSpaces
        .AsNoTracking()
        .Where(bs => bs.BookingId == id)
        .Select(bs => bs.SpaceId)
        .ToList();

    // Run conflict detection if booking has spaces
    if (existingSpaceIds.Count > 0)
    {
        var (conflictingBookingIds, conflictingSpaceIds) = FindConflictingBookings(
            db,
            tenant.CompanyId,
            request.StartUtc,
            request.EndUtc,
            existingSpaceIds,
            id); // Exclude current booking

        if (conflictingBookingIds.Count > 0)
        {
            return Results.Conflict(new BookingConflictResponse(
                "Booking conflicts with existing bookings.",
                conflictingBookingIds,
                conflictingSpaceIds));
        }
    }

    // Update booking fields using type-safe domain method
    booking.UpdateDetails(request.Title, request.StartUtc, request.EndUtc, request.AttendeeCount);

    // Update SpaceConfigurationId via type-safe setter
    booking.SetSpaceConfigurationId(request.SpaceConfigurationId);

    // Recalculate TotalAmount if booking has spaces
    if (existingSpaceIds.Count > 0)
    {
        // Load space hourly rates
        var spaceHourlyRates = db.Spaces
            .AsNoTracking()
            .Where(s => existingSpaceIds.Contains(s.Id))
            .Select(s => s.HourlyRate)
            .ToList();

        // Load space configuration override rate if set
        decimal? overrideRate = null;
        if (request.SpaceConfigurationId.HasValue)
        {
            overrideRate = db.SpaceConfigurations
                .AsNoTracking()
                .Where(sc => sc.Id == request.SpaceConfigurationId.Value)
                .Select(sc => sc.HourlyRateOverride)
                .FirstOrDefault();
        }

        var totalAmount = CalculateBookingTotal(request.StartUtc, request.EndUtc, spaceHourlyRates, overrideRate);
        booking.SetTotalAmount(totalAmount);
    }

    await db.SaveChangesAsync();

    // Return updated space IDs
    var updatedSpaceIds = db.BookingSpaces
        .AsNoTracking()
        .Where(bs => bs.BookingId == id)
        .Select(bs => bs.SpaceId)
        .ToList();

    return Results.Ok(new BookingResponse(
        booking.Id,
        booking.ClientId,
        booking.Title,
        booking.StartUtc,
        booking.EndUtc,
        booking.AttendeeCount,
        booking.IsCancelled,
        updatedSpaceIds,
        booking.TotalAmount,
        booking.SpaceConfigurationId,
        booking.Status,
        booking.CancelledUtc,
        booking.CancelReason));
})
.RequireAuthorization();

// PUT /{companySlug}/bookings/{id}/spaces - Replace spaces for a booking (requires auth + Manager/Admin/Owner)
tenantGroup.MapPut("/bookings/{id:guid}/spaces", async (ApplicationDbContext db, ITenantContext tenantContext, ClaimsPrincipal user, Guid id, SetBookingSpacesRequest request) =>
{
    // Extract userId from "sub" claim
    var userIdClaim = user.FindFirst("sub")?.Value;
    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var parsedUserId))
    {
        return Results.Unauthorized();
    }

    var tenant = tenantContext.Current;
    if (tenant is null)
    {
        return Results.NotFound();
    }

    // Query membership with role
    var membership = db.UserCompanyMemberships
        .AsNoTracking()
        .FirstOrDefault(m => m.UserId == parsedUserId && m.CompanyId == tenant.CompanyId);

    if (membership is null)
    {
        return Results.Forbid();
    }

    // Allow only: CompanyOwner, CompanyAdmin, CompanyManager
    var allowedRoles = new[] { TenantRoles.CompanyOwner, TenantRoles.CompanyAdmin, TenantRoles.CompanyManager };
    if (!allowedRoles.Contains(membership.Role))
    {
        return Results.Forbid();
    }

    // Load booking (tenant-filtered)
    var booking = db.Bookings
        .FirstOrDefault(b => b.Id == id && b.CompanyId == tenant.CompanyId);

    if (booking is null)
    {
        return Results.NotFound();
    }

    // Reject updates to cancelled bookings
    if (booking.IsCancelled)
    {
        return Results.BadRequest(new { error = "Cancelled booking cannot be modified." });
    }

    // Reject updates to confirmed bookings
    if (booking.Status == BookingStatus.Confirmed)
    {
        return Results.BadRequest(new { error = "Confirmed booking cannot be modified." });
    }

    // If requested SpaceIds is empty -> clear and return (no conflict check needed)
    if (request.SpaceIds.Count == 0)
    {
        // Remove existing associations
        var existingAssociations = db.BookingSpaces
            .Where(bs => bs.BookingId == id)
            .ToList();

        db.BookingSpaces.RemoveRange(existingAssociations);

        // Set TotalAmount to 0 when clearing spaces
        booking.SetTotalAmount(0);

        await db.SaveChangesAsync();

        return Results.Ok(new BookingResponse(
            booking.Id,
            booking.ClientId,
            booking.Title,
            booking.StartUtc,
            booking.EndUtc,
            booking.AttendeeCount,
            booking.IsCancelled,
            new List<Guid>(),
            booking.TotalAmount,
            booking.SpaceConfigurationId,
            booking.Status,
            booking.CancelledUtc,
            booking.CancelReason));
    }

    // Validate that all space IDs exist and belong to this tenant
    var requestedSpaceIds = request.SpaceIds.ToHashSet();
    var existingSpaces = db.Spaces
        .AsNoTracking()
        .Where(s => requestedSpaceIds.Contains(s.Id) && s.CompanyId == tenant.CompanyId)
        .Select(s => s.Id)
        .ToList();

    if (existingSpaces.Count != requestedSpaceIds.Count)
    {
        var missingIds = requestedSpaceIds.Except(existingSpaces);
        return Results.BadRequest(new { error = "One or more space IDs are invalid or do not belong to this tenant.", missingIds });
    }

    // Run conflict detection using booking's time range and requested spaces
    var (conflictingBookingIds, conflictingSpaceIds) = FindConflictingBookings(
        db,
        tenant.CompanyId,
        booking.StartUtc,
        booking.EndUtc,
        requestedSpaceIds,
        id); // Exclude current booking

    if (conflictingBookingIds.Count > 0)
    {
        return Results.Conflict(new BookingConflictResponse(
            "Booking conflicts with existing bookings.",
            conflictingBookingIds,
            conflictingSpaceIds));
    }

    // Remove existing associations
    var existingAssoc = db.BookingSpaces
        .Where(bs => bs.BookingId == id)
        .ToList();

    db.BookingSpaces.RemoveRange(existingAssoc);

    // Add new associations
    foreach (var spaceId in request.SpaceIds)
    {
        db.BookingSpaces.Add(new BookingSpace
        {
            BookingId = id,
            SpaceId = spaceId
        });
    }

    // Load space hourly rates and calculate TotalAmount
    var spaceHourlyRates = db.Spaces
        .AsNoTracking()
        .Where(s => requestedSpaceIds.Contains(s.Id))
        .Select(s => s.HourlyRate)
        .ToList();

    // Load space configuration override rate if set
    decimal? overrideRate = null;
    if (booking.SpaceConfigurationId.HasValue)
    {
        overrideRate = db.SpaceConfigurations
            .AsNoTracking()
            .Where(sc => sc.Id == booking.SpaceConfigurationId.Value)
            .Select(sc => sc.HourlyRateOverride)
            .FirstOrDefault();
    }

    var totalAmount = CalculateBookingTotal(booking.StartUtc, booking.EndUtc, spaceHourlyRates, overrideRate);
    booking.SetTotalAmount(totalAmount);

    await db.SaveChangesAsync();

    // Return updated space IDs
    var updatedSpaceIds = db.BookingSpaces
        .AsNoTracking()
        .Where(bs => bs.BookingId == id)
        .Select(bs => bs.SpaceId)
        .ToList();

    return Results.Ok(new BookingResponse(
        booking.Id,
        booking.ClientId,
        booking.Title,
        booking.StartUtc,
        booking.EndUtc,
        booking.AttendeeCount,
        booking.IsCancelled,
        updatedSpaceIds,
        booking.TotalAmount,
        booking.SpaceConfigurationId,
        booking.Status,
        booking.CancelledUtc,
        booking.CancelReason));
})
.RequireAuthorization();

// PUT /{companySlug}/bookings/{id}/with-spaces - Update booking fields and replace spaces atomically (requires auth + Manager/Admin/Owner)
tenantGroup.MapPut("/bookings/{id:guid}/with-spaces", async (ApplicationDbContext db, ITenantContext tenantContext, ClaimsPrincipal user, Guid id, UpdateBookingWithSpacesRequest request) =>
{
    // Extract userId from "sub" claim
    var userIdClaim = user.FindFirst("sub")?.Value;
    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
    {
        return Results.Unauthorized();
    }

    var tenant = tenantContext.Current;
    if (tenant is null)
    {
        return Results.NotFound();
    }

    // Query membership with role
    var membership = db.UserCompanyMemberships
        .AsNoTracking()
        .FirstOrDefault(m => m.UserId == userId && m.CompanyId == tenant.CompanyId);

    if (membership is null)
    {
        return Results.Forbid();
    }

    // Allow only: CompanyOwner, CompanyAdmin, CompanyManager
    var allowedRoles = new[] { TenantRoles.CompanyOwner, TenantRoles.CompanyAdmin, TenantRoles.CompanyManager };
    if (!allowedRoles.Contains(membership.Role))
    {
        return Results.Forbid();
    }

    // 1) Load booking (tenant-filtered)
    var booking = db.Bookings
        .FirstOrDefault(b => b.Id == id && b.CompanyId == tenant.CompanyId);

    if (booking is null)
    {
        return Results.NotFound();
    }

    // Reject updates to cancelled bookings
    if (booking.IsCancelled)
    {
        return Results.BadRequest(new { error = "Cancelled booking cannot be modified." });
    }

    // Reject updates to confirmed bookings
    if (booking.Status == BookingStatus.Confirmed)
    {
        return Results.BadRequest(new { error = "Confirmed booking cannot be modified." });
    }

    // 2) Validate basic fields
    if (string.IsNullOrWhiteSpace(request.Title))
    {
        return Results.BadRequest(new { error = "Title is required." });
    }
    if (request.Title.Length > 200)
    {
        return Results.BadRequest(new { error = "Title cannot exceed 200 characters." });
    }
    if (request.StartUtc >= request.EndUtc)
    {
        return Results.BadRequest(new { error = "Start time must be before end time." });
    }
    if (request.AttendeeCount < 0)
    {
        return Results.BadRequest(new { error = "Attendee count cannot be negative." });
    }

    // 3) Validate SpaceConfigurationId if provided
    if (request.SpaceConfigurationId.HasValue)
    {
        var configExists = db.SpaceConfigurations
            .AsNoTracking()
            .Any(sc => sc.Id == request.SpaceConfigurationId.Value && sc.CompanyId == tenant.CompanyId && sc.IsActive);

        if (!configExists)
        {
            return Results.BadRequest(new { error = "Space configuration not found, does not belong to this tenant, or is not active." });
        }
    }

    // 4) Validate minimum booking duration
    var (durationOk, minMinutes) = await ValidateMinBookingDurationAsync(
        request.SpaceConfigurationId,
        request.StartUtc,
        request.EndUtc,
        db);

    if (!durationOk)
    {
        return Results.BadRequest(new {
            error = "Booking duration is below the minimum allowed.",
            minMinutes = minMinutes
        });
    }

    // 5) Validate SpaceIds
    if (request.SpaceIds.Count == 0)
    {
        return Results.BadRequest(new { error = "SpaceIds is required and must not be empty." });
    }

    // Deduplicate SpaceIds
    var dedupedSpaceIds = request.SpaceIds.ToHashSet();

    // Ensure all spaces exist in tenant
    var existingSpaceIds = db.Spaces
        .AsNoTracking()
        .Where(s => dedupedSpaceIds.Contains(s.Id) && s.CompanyId == tenant.CompanyId)
        .Select(s => s.Id)
        .ToList();

    if (existingSpaceIds.Count != dedupedSpaceIds.Count)
    {
        var missingIds = dedupedSpaceIds.Except(existingSpaceIds).ToList();
        return Results.BadRequest(new { error = "One or more space IDs are invalid or do not belong to this tenant.", missingSpaceIds = missingIds });
    }

    // 6) Conflict detection (exclude current booking)
    var (conflictingBookingIds, conflictingSpaceIds) = FindConflictingBookings(
        db,
        tenant.CompanyId,
        request.StartUtc,
        request.EndUtc,
        dedupedSpaceIds,
        id); // Exclude current booking

    if (conflictingBookingIds.Count > 0)
    {
        return Results.Conflict(new BookingConflictResponse(
            "Booking conflicts with existing bookings.",
            conflictingBookingIds,
            conflictingSpaceIds));
    }

    // 7) Replace spaces: load existing join rows, compute toRemove/toAdd
    var existingBookingSpaceIds = db.BookingSpaces
        .Where(bs => bs.BookingId == id)
        .Select(bs => bs.SpaceId)
        .ToHashSet();

    var toRemove = existingBookingSpaceIds.Except(dedupedSpaceIds).ToList();
    var toAdd = dedupedSpaceIds.Except(existingBookingSpaceIds).ToList();

    // Remove associations no longer needed
    if (toRemove.Count > 0)
    {
        var removeAssociations = db.BookingSpaces
            .Where(bs => bs.BookingId == id && toRemove.Contains(bs.SpaceId))
            .ToList();
        db.BookingSpaces.RemoveRange(removeAssociations);
    }

    // Add new associations
    foreach (var spaceId in toAdd)
    {
        db.BookingSpaces.Add(new BookingSpace
        {
            BookingId = id,
            SpaceId = spaceId
        });
    }

    // 8) Update booking fields using type-safe domain method
    booking.UpdateDetails(request.Title, request.StartUtc, request.EndUtc, request.AttendeeCount);

    // Update SpaceConfigurationId via type-safe setter
    booking.SetSpaceConfigurationId(request.SpaceConfigurationId);

    // 9) Recalculate TotalAmount
    // Load hourly rates for requested spaces
    var spaceHourlyRates = db.Spaces
        .AsNoTracking()
        .Where(s => dedupedSpaceIds.Contains(s.Id))
        .Select(s => s.HourlyRate)
        .ToList();

    // Load override hourly rate from SpaceConfiguration if set
    decimal? overrideRate = null;
    if (request.SpaceConfigurationId.HasValue)
    {
        overrideRate = db.SpaceConfigurations
            .AsNoTracking()
            .Where(sc => sc.Id == request.SpaceConfigurationId.Value)
            .Select(sc => sc.HourlyRateOverride)
            .FirstOrDefault();
    }

    var totalAmount = CalculateBookingTotal(request.StartUtc, request.EndUtc, spaceHourlyRates, overrideRate);
    booking.SetTotalAmount(totalAmount);

    // 10) SaveChangesAsync (single call for atomicity)
    await db.SaveChangesAsync();

    // 11) Return 200 OK with BookingResponse
    return Results.Ok(new BookingResponse(
        booking.Id,
        booking.ClientId,
        booking.Title,
        booking.StartUtc,
        booking.EndUtc,
        booking.AttendeeCount,
        booking.IsCancelled,
        dedupedSpaceIds.ToList(),
        booking.TotalAmount,
        booking.SpaceConfigurationId,
        booking.Status,
        booking.CancelledUtc,
        booking.CancelReason));
})
.RequireAuthorization();

// Apply migrations and seed minimal data
using (var scope = app.Services.CreateScope())
{
var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
db.Database.Migrate();

const string seedSlug = "acme";
if (!db.Companies.Any(c => c.Slug == seedSlug))
{
    db.Companies.Add(new Company("Acme Venue", seedSlug));
    db.SaveChanges();
}
}

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

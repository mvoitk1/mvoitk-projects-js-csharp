using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using VenuePlatform.BLL.Domain.Auth;
using VenuePlatform.BLL.Domain.Clients;
using VenuePlatform.BLL.Domain.Companies;
using VenuePlatform.BLL.Domain.Spaces;
using VenuePlatform.BLL.Tenancy;
using VenuePlatform.Contracts.Auth;
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
            return Results.BadRequest(new { error = "Slug must be 2-64 chars and contain only a-z, 0-9, and '-'" });
        }
        foreach (var c in slug)
        {
            if (!char.IsAsciiLetterLower(c) && !char.IsAsciiDigit(c) && c != '-')
            {
                return Results.BadRequest(new { error = "Slug must be 2-64 chars and contain only a-z, 0-9, and '-'" });
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

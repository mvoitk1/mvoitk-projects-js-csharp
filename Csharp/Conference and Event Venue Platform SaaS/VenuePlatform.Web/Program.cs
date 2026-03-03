using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using VenuePlatform.BLL.Domain.Auth;
using VenuePlatform.BLL.Domain.Companies;
using VenuePlatform.BLL.Tenancy;
using VenuePlatform.DAL.Persistence;
using VenuePlatform.Web.Auth;
using VenuePlatform.Web.Endpoints;
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
    app.MapDevEndpoints();
}

app.UseHttpsRedirection();

// Authentication & Authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// Tenant resolution middleware - must be before endpoints
app.UseMiddleware<TenantResolutionMiddleware>();

// Platform-level endpoints (no tenant required)
app.MapPlatformHealthEndpoints();
app.MapAuthEndpoints(builder.Configuration);

// Tenant route group - endpoints under /{companySlug}
var tenantGroup = app.MapGroup("/{companySlug}");

// Tenant-scoped endpoints
tenantGroup.MapTenantHealthEndpoints();
tenantGroup.MapClientEndpoints();
tenantGroup.MapSpaceEndpoints();
tenantGroup.MapSpaceConfigurationEndpoints();
tenantGroup.MapBookingEndpoints();

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

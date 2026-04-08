using App.BLL;
using WebApp.Helpers;
using WebApp.Services;
using WebApp.Setup;

// Create the startup builder with configuration, logging, DI, and web host defaults.
var builder = WebApplication.CreateBuilder(args);

// Service registration
builder.Services.AddAppDatabase(builder.Configuration, builder.Environment); // Register AppDbContext using configuration and environment settings.
builder.Services.AddAppIdentity(); // Add ASP.NET Core Identity for users, roles, and auth tokens.
builder.Services.AddHttpClient(); // Enable HttpClient injection for outbound HTTP calls.
builder.Services.AddSingleton<AppNameService>(); // Register a single shared app name service instance.
builder.Services.AddVenuePlatformServices(); // Register application and business-layer services.
builder.Services.AddScoped<IVenueOperatorIdentityRoleSyncService, VenueOperatorIdentityRoleSyncService>(); // Sync venue operator identity roles per request.
builder.Services.AddAppControllers(); // Add MVC controllers/views and JSON serialization settings.
builder.Services.AddForwardedHeaders(); // Trust forwarded headers when running behind a reverse proxy.
builder.Services.AddAppCors(); // Configure cross-origin request policy.
builder.Services.AddAppApiVersioning(); // Add API versioning support and version explorer.
builder.Services.AddAppSwagger(); // Register Swagger/OpenAPI services.
builder.Services.AddAppLocalization(builder.Configuration); // Load supported cultures and localization options from configuration.

// Build and configure pipeline
// Build the configured services into the runnable web application.
var app = builder.Build();

app.SetupAppData(); // Run startup data tasks like database migration and seeding.
app.UseAppMiddleware(); // Add middleware to the HTTP request pipeline.
app.UseAppSwagger(); // Enable Swagger JSON and the Swagger UI.
app.MapAppEndpoints(); // Map static assets, MVC routes, and Razor Pages endpoints.

// Start the web server and begin handling incoming requests.
app.Run();

// this is needed for unit testing
// ReSharper disable once ClassNeverInstantiated.Global
public partial class Program
{
}

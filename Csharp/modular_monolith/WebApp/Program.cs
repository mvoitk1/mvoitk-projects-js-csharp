using App.DAL.EF;
using Catalog.Infrastructure;
using Catalog.Module;
using Microsoft.Extensions.DependencyInjection;
using Modules.Abstractions;
using Modules.SharedKernel;
using Sales.Infrastructure;
using Sales.Module;
using Users.Infrastructure;
using Users.Module;
using WebApp.Helpers;
using WebApp.Setup;

var builder = WebApplication.CreateBuilder(args);

// Modular monolith composition: each module owns its own DbContext, DI, controllers.
IModule[] modules =
[
    new UsersModule(),
    new CatalogModule(),
    new SalesModule(),
];

builder.Services.AddAppDatabase(builder.Configuration, builder.Environment);
foreach (var module in modules)
{
    module.Register(builder.Services, builder.Configuration);
}

builder.Services.AddAppServices();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<AppNameService>();
builder.Services.AddAppControllers();
builder.Services.AddForwardedHeaders();
builder.Services.AddAppCors();
builder.Services.AddAppApiVersioning();
builder.Services.AddAppSwagger();
builder.Services.AddAppLocalization(builder.Configuration);
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>()
    .AddDbContextCheck<UsersDbContext>()
    .AddDbContextCheck<CatalogDbContext>()
    .AddDbContextCheck<SalesDbContext>();

// Build and configure pipeline
var app = builder.Build();

await app.SetupAppDataAsync();
app.UseAppMiddleware();
app.UseAppSwagger();
app.MapAppEndpoints();
app.MapHealthChecks("/health");

app.Run();

// this is needed for unit testing
// ReSharper disable once ClassNeverInstantiated.Global
public partial class Program
{
}

using App.DAL.EF;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Helpers;
using WebApp.Setup;

var builder = WebApplication.CreateBuilder(args);

// Service registration
builder.Services.AddAppDatabase(builder.Configuration, builder.Environment);
builder.Services.AddAppIdentity(builder.Configuration);
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
    .AddDbContextCheck<AppDbContext>();

// Build and configure pipeline
var app = builder.Build();

app.SetupAppData();
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

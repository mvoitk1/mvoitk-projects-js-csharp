using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace App.DAL.EF;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = ResolveConnectionString();
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

        optionsBuilder.UseNpgsql(connectionString);

        return new AppDbContext(optionsBuilder.Options);
    }

    private static string ResolveConnectionString()
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var appSettingsPath = Path.Combine(currentDirectory, "WebApp", "appsettings.json");

        if (!File.Exists(appSettingsPath))
        {
            appSettingsPath = Path.Combine(currentDirectory, "..", "WebApp", "appsettings.json");
        }

        if (!File.Exists(appSettingsPath))
        {
            throw new InvalidOperationException("Unable to locate WebApp/appsettings.json for design-time DbContext creation.");
        }

        using var stream = File.OpenRead(appSettingsPath);
        using var document = JsonDocument.Parse(stream);

        if (document.RootElement.TryGetProperty("ConnectionStrings", out var connectionStrings) &&
            connectionStrings.TryGetProperty("DefaultConnection", out var defaultConnection))
        {
            return defaultConnection.GetString()
                   ?? throw new InvalidOperationException("DefaultConnection is missing from appsettings.json.");
        }

        throw new InvalidOperationException("ConnectionStrings:DefaultConnection is missing from appsettings.json.");
    }
}

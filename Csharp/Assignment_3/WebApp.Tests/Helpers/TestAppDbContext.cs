using System.Text.Json;
using App.DAL.EF;
using App.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace WebApp.Tests.Helpers;

/// <summary>
/// Serialises a <see cref="LangStr"/> to / from a JSON string.
/// </summary>
public class LangStrConverter : ValueConverter<LangStr, string>
{
    public LangStrConverter() : base(
        v => JsonSerializer.Serialize(new Dictionary<string, string>(v), (JsonSerializerOptions?)null),
        v => Deserialize(v))
    {
    }

    private static LangStr Deserialize(string json)
    {
        var result = new LangStr();
        var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
        if (dict != null)
        {
            foreach (var (key, value) in dict)
                result[key] = value;
        }
        return result;
    }
}

/// <summary>
/// Test-only <see cref="AppDbContext"/>. In production Npgsql maps <see cref="LangStr"/> straight
/// to a <c>jsonb</c> column, but under the EF Core InMemory provider <c>LangStr</c> is otherwise
/// discovered as a separate entity type and translatable fields fail to round-trip between context
/// instances. Registering a value converter via <see cref="ConfigureConventions"/> makes EF treat
/// <c>LangStr</c> as a plain JSON-serialised scalar instead. Production code is left untouched.
/// </summary>
public class TestAppDbContext : AppDbContext
{
    public TestAppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        configurationBuilder.Properties<LangStr>().HaveConversion<LangStrConverter>();
    }
}

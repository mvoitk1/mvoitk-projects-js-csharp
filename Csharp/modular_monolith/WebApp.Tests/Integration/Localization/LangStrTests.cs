using System.Globalization;
using System.Threading;
using Modules.SharedKernel;
using Xunit;

namespace WebApp.Tests.Integration.Localization;

// Note: LangStr is exposed end-to-end via PostgreSQL jsonb. The InMemory EF provider
// used by CustomWebApplicationFactory does not preserve the Dictionary contents on
// query, so a true API round-trip cannot be exercised here. These tests assert the
// translation logic the API mappers rely on (`LangStr.Translate(culture)`) so the
// fallback contract is locked down regardless of the DB provider.
public class LangStrTests
{
    public LangStrTests()
    {
        LangStr.DefaultCulture = "en";
    }

    [Fact]
    public void Translate_KnownCulture_ReturnsThatLanguage()
    {
        var s = new LangStr("Hello", "en") { ["et"] = "Tere" };

        Assert.Equal("Hello", s.Translate("en"));
        Assert.Equal("Tere", s.Translate("et"));
    }

    [Fact]
    public void Translate_RegionVariant_FallsBackToNeutralCulture()
    {
        var s = new LangStr("Hello", "en") { ["et"] = "Tere" };

        Assert.Equal("Tere", s.Translate("et-EE"));
        Assert.Equal("Hello", s.Translate("en-US"));
    }

    [Fact]
    public void Translate_UnknownCulture_FallsBackToDefaultCulture()
    {
        var s = new LangStr("Hello", "en") { ["et"] = "Tere" };

        var result = s.Translate("fr");

        Assert.Equal("Hello", result);
    }

    [Fact]
    public void Translate_UsesAmbientThreadCultureWhenCultureIsNull()
    {
        var s = new LangStr("Hello", "en") { ["et"] = "Tere" };
        var original = Thread.CurrentThread.CurrentUICulture;
        try
        {
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("et-EE");
            Assert.Equal("Tere", s.Translate());

            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en");
            Assert.Equal("Hello", s.Translate());
        }
        finally
        {
            Thread.CurrentThread.CurrentUICulture = original;
        }
    }

    [Fact]
    public void Translate_EmptyDictionary_ReturnsNull()
    {
        var s = new LangStr();

        Assert.Null(s.Translate("en"));
    }
}

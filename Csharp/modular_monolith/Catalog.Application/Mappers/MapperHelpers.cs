using Catalog.Domain;
using Modules.SharedKernel;

namespace Catalog.Application.Mappers;

/// <summary>Shared low-level helpers used by the mapper classes.</summary>
internal static class MapperHelpers
{
    private static readonly TimeZoneInfo Tz =
        TimeZoneInfo.FindSystemTimeZoneById("Europe/Tallinn");

    /// <summary>Translate a <see cref="LangStr"/> for the current culture, never null.</summary>
    public static string Tr(this LangStr str) => str.Translate() ?? string.Empty;

    /// <summary>Read the raw value for a specific culture key, never null.</summary>
    public static string Lang(this LangStr str, string culture) =>
        str.ContainsKey(culture) ? str[culture] : string.Empty;

    /// <summary>Convert a stored UTC timestamp to Europe/Tallinn local time for display.</summary>
    public static DateTime ToLocal(DateTime utc) => TimeZoneInfo.ConvertTimeFromUtc(utc, Tz);
}

using System.Text.Json;

namespace Modules.SharedKernel.Mapping;

/// <summary>
/// Boundary mapper between Application DTOs and module-scoped public-API DTOs.
/// Both DTO surfaces share property names by convention, so a JSON roundtrip
/// copies every field exactly once. The seam exists so the wire format can
/// version independently of the Application contract.
/// </summary>
public static class ApiDtoMappers
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static TTarget MapTo<TTarget>(this object source)
        where TTarget : class
    {
        var json = JsonSerializer.Serialize(source, source.GetType(), Options);
        return JsonSerializer.Deserialize<TTarget>(json, Options)!;
    }

    public static List<TTarget> MapList<TTarget>(this IEnumerable<object> source)
        where TTarget : class
    {
        return source.Select(s => s.MapTo<TTarget>()).ToList();
    }
}

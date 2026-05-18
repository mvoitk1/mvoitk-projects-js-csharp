using System.Text.Json;

namespace App.DTO.Mappers;

/// <summary>
/// Boundary mapper between BLL DTOs (<c>App.BLL.DTO</c>) and the versioned public API
/// DTOs (<c>App.DTO.v1</c>). The two surfaces share property names by convention so a
/// JSON roundtrip copies every field exactly once. The seam exists so that the API
/// can version independently of the BLL contract.
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

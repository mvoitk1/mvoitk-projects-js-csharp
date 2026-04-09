using System.Text.Json;

namespace Tests.Helpers;

public static class JsonHelper
{
    public static JsonSerializerOptions CamelCase = new JsonSerializerOptions()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}
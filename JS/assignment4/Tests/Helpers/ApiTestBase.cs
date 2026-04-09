using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using PublicApi.DTO.v1.Identity;

namespace Tests.Helpers;

public static class ApiTestBase
{
    public static async Task<JwtResponse> LoginAsync(HttpClient client, string email, string password,
        int? expiresInSeconds = null)
    {
        var url = "/api/v1.0/Account/Login";
        if (expiresInSeconds.HasValue)
        {
            url += $"?expiresInSeconds={expiresInSeconds.Value}";
        }

        var response = await client.PostAsJsonAsync(url, new { email, password });
        var contentStr = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        var loginData = JsonSerializer.Deserialize<JwtResponse>(contentStr, JsonHelper.CamelCase);
        return loginData!;
    }

    public static async Task<JwtResponse> RegisterAsync(HttpClient client, string email, string password,
        string firstName, string lastName, int? expiresInSeconds = null)
    {
        var url = "/api/v1.0/Account/Register";
        if (expiresInSeconds.HasValue)
        {
            url += $"?expiresInSeconds={expiresInSeconds.Value}";
        }

        var response = await client.PostAsJsonAsync(url,
            new { email, password, firstName, lastName });
        var contentStr = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        var registerData = JsonSerializer.Deserialize<JwtResponse>(contentStr, JsonHelper.CamelCase);
        return registerData!;
    }

    public static HttpRequestMessage CreateAuthMessage(HttpMethod method, string url, string token)
    {
        var msg = new HttpRequestMessage(method, url);
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        msg.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return msg;
    }
}

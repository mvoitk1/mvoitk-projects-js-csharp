using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using App.DTO.v1.Identity;
using App.Helpers;
using AngleSharp.Html.Dom;
using Xunit;

namespace WebApp.Tests.Helpers;

public static class IdentityHelper
{
    public static async Task<JWTResponse> SetupUserAsync(HttpClient httpClient , string firstName, string lastName, string password, string email)
    {
        var data = new Register()
        {
            Password = password,
            Email = email,
        };

        // Act
        var response = await httpClient.PostAsync(
            "/api/v1/account/register",
            new StringContent(
                System.Text.Json.JsonSerializer.Serialize(data, JsonHelpers.JsonSerializerOptionsCamelCase),
                Encoding.UTF8, "application/json")
        );

        var responseString = await response.Content.ReadAsStringAsync();
        
        // Assert
        response.EnsureSuccessStatusCode();
        
        var jwtResponse = System.Text.Json.JsonSerializer.Deserialize<JWTResponse>(responseString, JsonHelpers.JsonSerializerOptionsCamelCase);

        Assert.NotNull(jwtResponse);

        return jwtResponse;
    }

    public static async Task LoginViaUiAsync(HttpClient httpClient, string email, string password)
    {
        var loginPage = await httpClient.GetAsync("/Identity/Account/Login");
        loginPage.EnsureSuccessStatusCode();

        var document = await HtmlHelpers.GetDocumentAsync(loginPage);
        var form = (IHtmlFormElement)Assert.Single(document.QuerySelectorAll("form"));

        var response = await httpClient.SendAsync(
            form,
            new Dictionary<string, string>
            {
                ["Input.Email"] = email,
                ["Input.Password"] = password,
                ["Input.RememberMe"] = bool.FalseString
            });

        Assert.True(
            response.IsSuccessStatusCode || response.StatusCode is System.Net.HttpStatusCode.Redirect or System.Net.HttpStatusCode.Found,
            $"Unexpected login status code: {response.StatusCode}");
    }
}

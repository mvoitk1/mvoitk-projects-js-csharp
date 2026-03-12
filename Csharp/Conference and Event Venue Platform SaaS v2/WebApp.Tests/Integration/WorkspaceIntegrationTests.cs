using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using AngleSharp.Html.Dom;
using App.DAL.EF;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Tests.Helpers;

namespace WebApp.Tests.Integration;

[Collection("Database tests")]
public class WorkspaceIntegrationTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private const string SeedPassword = "Kala.Maja.101";
    private readonly CustomWebApplicationFactory<Program> _factory;

    public WorkspaceIntegrationTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task EmployeeDashboard_AnonymousUserRedirectsToLogin()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/Employee/Dashboard");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Identity/Account/Login", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task WorkspaceRoute_EmployeeRedirectsToEmployeeDashboard()
    {
        using var client = CreateClient();
        await IdentityHelper.LoginViaUiAsync(client, "employee@northstarvenues.test", SeedPassword);

        var response = await client.GetAsync("/workspace");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Employee/Dashboard", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task VenueRequestForm_SubmitsForAuthenticatedRequester()
    {
        using var client = CreateClient();
        await IdentityHelper.LoginViaUiAsync(client, "requester@newvenue.test", SeedPassword);

        var beforeCount = await CountVenueRequestsAsync();

        var requestPage = await client.GetAsync("/venues/request");
        requestPage.EnsureSuccessStatusCode();
        var document = await HtmlHelpers.GetDocumentAsync(requestPage);
        var form = (IHtmlFormElement)Assert.Single(document.QuerySelectorAll("form"));

        var postResponse = await client.SendAsync(
            form,
            new Dictionary<string, string>
            {
                ["CompanyName"] = "Meridian Events",
                ["VenueName"] = "Meridian Hall",
                ["ContactName"] = "Kai Ruut",
                ["ContactEmail"] = "kai@meridian.test",
                ["ContactPhone"] = "+3725000001",
                ["EstimatedMonthlyBookings"] = "18",
                ["City"] = "Tallinn",
                ["Country"] = "Estonia",
                ["AddressLine1"] = "Narva mnt 2",
                ["Notes"] = "Need onboarding for a waterfront launch venue."
            });

        Assert.Equal(HttpStatusCode.Redirect, postResponse.StatusCode);

        var afterCount = await CountVenueRequestsAsync();
        Assert.Equal(beforeCount + 1, afterCount);
    }

    [Fact]
    public async Task ManagerCanSwitchVenueContextFromWorkspace()
    {
        using var client = CreateClient();
        await IdentityHelper.LoginViaUiAsync(client, "manager@northstarvenues.test", SeedPassword);

        var venue = await GetVenueBySlugAsync("harbor-hall");
        var dashboardPage = await client.GetAsync("/Admin/Dashboard");
        dashboardPage.EnsureSuccessStatusCode();

        var document = await HtmlHelpers.GetDocumentAsync(dashboardPage);
        var form = Assert.Single(document.QuerySelectorAll("form").OfType<IHtmlFormElement>());

        var switchResponse = await client.SendAsync(
            form,
            new Dictionary<string, string>
            {
                ["venueId"] = venue.Id.ToString()
            });

        Assert.Equal(HttpStatusCode.Redirect, switchResponse.StatusCode);

        var updatedDashboard = await client.GetAsync("/Admin/Dashboard");
        updatedDashboard.EnsureSuccessStatusCode();
        var markup = await updatedDashboard.Content.ReadAsStringAsync();
        Assert.Contains("Harbor Hall", markup);
    }

    [Fact]
    public async Task AdminRequests_AreRestrictedToPlatformAdmins()
    {
        using var employeeClient = CreateClient();
        await IdentityHelper.LoginViaUiAsync(employeeClient, "employee@northstarvenues.test", SeedPassword);

        var forbiddenResponse = await employeeClient.GetAsync("/Admin/Requests");
        Assert.Equal(HttpStatusCode.Redirect, forbiddenResponse.StatusCode);
        Assert.Contains("/Identity/Account/AccessDenied", forbiddenResponse.Headers.Location?.ToString());

        using var adminClient = CreateClient();
        await IdentityHelper.LoginViaUiAsync(adminClient, "akaver@akaver.com", SeedPassword);

        var adminResponse = await adminClient.GetAsync("/Admin/Requests");
        adminResponse.EnsureSuccessStatusCode();
        var markup = await adminResponse.Content.ReadAsStringAsync();
        Assert.Contains("Lighthouse Forum", markup);
    }

    private HttpClient CreateClient() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

    private async Task<int> CountVenueRequestsAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await Task.FromResult(db.VenueAccessRequests.Count());
    }

    private async Task<App.Domain.Venues.Venue> GetVenueBySlugAsync(string slug)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await Task.FromResult(db.Venues.Single(venue => venue.Slug == slug));
    }
}

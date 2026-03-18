using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using AngleSharp.Html.Dom;
using App.DAL.EF;
using App.Domain.Venues;
using App.Domain.Identity;
using App.Domain.ValueObjects;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
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
    public async Task LoginPage_AdminWithoutReturnUrlRedirectsToWorkspace()
    {
        using var client = CreateClient();

        var loginPage = await client.GetAsync("/Identity/Account/Login");
        loginPage.EnsureSuccessStatusCode();

        var document = await HtmlHelpers.GetDocumentAsync(loginPage);
        var form = (IHtmlFormElement)Assert.Single(document.QuerySelectorAll("form"));

        var response = await client.SendAsync(
            form,
            new Dictionary<string, string>
            {
                ["Input.Email"] = "akaver@akaver.com",
                ["Input.Password"] = SeedPassword,
                ["Input.RememberMe"] = bool.FalseString
            });

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/workspace", response.Headers.Location?.ToString());
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
        var form = Assert.Single(
            document.QuerySelectorAll("form").OfType<IHtmlFormElement>(),
            item => item.Action.EndsWith("/venues/request"));

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
    public async Task BrowseVenues_VenueCardLinksToVenueSpacesPage()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/venues");
        response.EnsureSuccessStatusCode();

        var document = await HtmlHelpers.GetDocumentAsync(response);
        var venueLink = Assert.Single(
            document.QuerySelectorAll("a.venue-card-link").OfType<IHtmlAnchorElement>(),
            item => item.PathName.EndsWith("/venues/northstar-conference-center"));

        var detailsResponse = await client.GetAsync(venueLink.Href);
        detailsResponse.EnsureSuccessStatusCode();

        var markup = await detailsResponse.Content.ReadAsStringAsync();
        Assert.Contains("Northstar Conference Center", markup);
        Assert.Contains("Aurora Hall", markup);
        Assert.Contains("Breakout Studio", markup);
    }

    [Fact]
    public async Task BrowseVenues_FilterByCityAndCapacityUpdatesResults()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/venues?city=Tartu&minimumCapacity=150");
        response.EnsureSuccessStatusCode();

        var document = await HtmlHelpers.GetDocumentAsync(response);
        var pageText = document.Body?.TextContent ?? string.Empty;

        Assert.Contains("Summit Riverside Hub", pageText);
        Assert.DoesNotContain("Northstar Conference Center", pageText);
        Assert.DoesNotContain("Harbor Hall", pageText);

        var citySelect = Assert.Single(document.QuerySelectorAll("select[name='city']").OfType<IHtmlSelectElement>());
        Assert.Equal("Tartu", citySelect.Value);

        var capacityInput = Assert.Single(document.QuerySelectorAll("input[name='minimumCapacity']").OfType<IHtmlInputElement>());
        Assert.Equal("150", capacityInput.Value);
    }

    [Fact]
    public async Task LandingPage_FeaturedVenueCardLinksToVenueSpacesPage()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/");
        response.EnsureSuccessStatusCode();

        var document = await HtmlHelpers.GetDocumentAsync(response);
        var venueLink = Assert.Single(
            document.QuerySelectorAll("a.venue-card-link").OfType<IHtmlAnchorElement>(),
            item => item.PathName.EndsWith("/venues/northstar-conference-center"));

        var ariaLabel = venueLink.GetAttribute("aria-label");
        Assert.NotNull(ariaLabel);
        Assert.Contains("Northstar Conference Center", ariaLabel);

        var detailsResponse = await client.GetAsync(venueLink.Href);
        detailsResponse.EnsureSuccessStatusCode();

        var markup = await detailsResponse.Content.ReadAsStringAsync();
        Assert.Contains("Northstar Conference Center", markup);
        Assert.Contains("Aurora Hall", markup);
    }

    [Fact]
    public async Task AuthenticatedUserCanSubmitBookingRequestFromVenueDetails()
    {
        using var client = CreateClient();
        await IdentityHelper.LoginViaUiAsync(client, "requester@newvenue.test", SeedPassword);

        var beforeCount = await CountBookingsAsync("northstar-conference-center");

        var detailsPage = await client.GetAsync("/venues/northstar-conference-center");
        detailsPage.EnsureSuccessStatusCode();

        var document = await HtmlHelpers.GetDocumentAsync(detailsPage);
        var form = Assert.Single(
            document.QuerySelectorAll("form").OfType<IHtmlFormElement>(),
            item => item.Action.EndsWith("/venues/northstar-conference-center/booking-request"));
        var tokenElement = form.QuerySelector("input[name='__RequestVerificationToken']");
        Assert.NotNull(tokenElement);
        var token = Assert.IsAssignableFrom<IHtmlInputElement>(tokenElement);

        var postResponse = await client.PostAsync(
            form.Action,
            new FormUrlEncodedContent(
            [
                new KeyValuePair<string, string>("__RequestVerificationToken", token!.Value),
                new KeyValuePair<string, string>("BookingRequest.SpaceId", (await GetSpaceIdByCodeAsync("northstar-conference-center", "AUR")).ToString()),
                new KeyValuePair<string, string>("BookingRequest.EventTitle", "Spring Strategy Day"),
                new KeyValuePair<string, string>("BookingRequest.ClientName", "Requester Co"),
                new KeyValuePair<string, string>("BookingRequest.StartsAt", DateTime.UtcNow.AddDays(15).ToString("yyyy-MM-ddTHH:mm")),
                new KeyValuePair<string, string>("BookingRequest.EndsAt", DateTime.UtcNow.AddDays(15).AddHours(6).ToString("yyyy-MM-ddTHH:mm")),
                new KeyValuePair<string, string>("BookingRequest.ExpectedAttendees", "60"),
                new KeyValuePair<string, string>("BookingRequest.SelectedCateringOptions", "Coffee and pastries - 8 EUR/person"),
                new KeyValuePair<string, string>("BookingRequest.SelectedCateringOptions", "Hot lunch buffet - 24 EUR/person"),
                new KeyValuePair<string, string>("BookingRequest.CateringNotes", "Coffee, pastries, buffet lunch."),
                new KeyValuePair<string, string>("BookingRequest.SelectedSetupOptions", "Projector and screen - 120 EUR/event"),
                new KeyValuePair<string, string>("BookingRequest.SelectedSetupOptions", "DJ booth - 280 EUR/event"),
                new KeyValuePair<string, string>("BookingRequest.SetupRequirements", "Classroom seating with stage screen."),
                new KeyValuePair<string, string>("BookingRequest.AdditionalRequirements", "Need sign-in desk and wheelchair aisle.")
            ]));

        Assert.Equal(HttpStatusCode.Redirect, postResponse.StatusCode);
        Assert.Equal("/venues/northstar-conference-center", postResponse.Headers.Location?.ToString());

        var afterCount = await CountBookingsAsync("northstar-conference-center");
        Assert.Equal(beforeCount + 1, afterCount);

        var createdBooking = await GetBookingByTitleAsync("Spring Strategy Day");
        Assert.Equal(BookingStatus.PendingApproval, createdBooking.Status);
        Assert.Equal("Spring Strategy Day", createdBooking.Title);
        Assert.Contains("Projector and screen - 120 EUR/event", createdBooking.CoordinationNotes);
        Assert.Contains("DJ booth - 280 EUR/event", createdBooking.CoordinationNotes);
        Assert.Single(createdBooking.CateringOrders);
        Assert.Contains("Coffee and pastries - 8 EUR/person", createdBooking.CateringOrders.Single().Notes);
        Assert.Contains("Hot lunch buffet - 24 EUR/person", createdBooking.CateringOrders.Single().Notes);
    }

    [Fact]
    public async Task ManagerCanApprovePendingBookingRequestFromCoordinationPage()
    {
        await SetManagerActiveVenueAsync("northstar-conference-center");
        var bookingId = await CreatePendingBookingRequestAsync();

        using var client = CreateClient();
        await IdentityHelper.LoginViaUiAsync(client, "manager@northstarvenues.test", SeedPassword);

        var page = await client.GetAsync($"/Employee/Coordination?bookingId={bookingId}");
        page.EnsureSuccessStatusCode();

        var document = await HtmlHelpers.GetDocumentAsync(page);
        var form = Assert.Single(
            document.QuerySelectorAll("form").OfType<IHtmlFormElement>(),
            item => item.Action.EndsWith("/Employee/Coordination/Approve"));

        var postResponse = await client.SendAsync(
            form,
            new Dictionary<string, string>
            {
                ["bookingId"] = bookingId.ToString()
            });

        Assert.Equal(HttpStatusCode.Redirect, postResponse.StatusCode);
        Assert.Equal($"/Employee/Coordination?bookingId={bookingId}", postResponse.Headers.Location?.ToString());

        var booking = await GetBookingAsync(bookingId);
        Assert.Equal(BookingStatus.Confirmed, booking.Status);
    }

    [Fact]
    public async Task EmployeeDashboard_ShowsIncomingBookingRequests()
    {
        await SetManagerActiveVenueAsync("northstar-conference-center");
        await CreatePendingBookingRequestAsync();

        using var client = CreateClient();
        await IdentityHelper.LoginViaUiAsync(client, "employee@northstarvenues.test", SeedPassword);

        var response = await client.GetAsync("/Employee/Dashboard");
        response.EnsureSuccessStatusCode();

        var markup = await response.Content.ReadAsStringAsync();
        Assert.Contains("Incoming booking requests", markup);
        Assert.Contains("Pending Approval Event", markup);
        Assert.Contains("Approve request", markup);
    }

    [Fact]
    public async Task ManagerDashboard_ShowsIncomingBookingRequests()
    {
        await SetManagerActiveVenueAsync("northstar-conference-center");
        await CreatePendingBookingRequestAsync();

        using var client = CreateClient();
        await IdentityHelper.LoginViaUiAsync(client, "manager@northstarvenues.test", SeedPassword);

        var response = await client.GetAsync("/Admin/Dashboard");
        response.EnsureSuccessStatusCode();

        var markup = await response.Content.ReadAsStringAsync();
        Assert.Contains("Incoming booking requests", markup);
        Assert.Contains("Pending Approval Event", markup);
        Assert.Contains("Approve request", markup);
        Assert.Contains("Venue occupancy calendar", markup);
        Assert.Contains("Duration", markup);
    }

    [Fact]
    public async Task WorkspaceBookingRequestDetails_ShowsApprovedStatusAndDueDateForRequester()
    {
        await ResetRequesterOperatorAccessAsync();
        var bookingId = await CreatePendingBookingRequestAsync("Requester Booking Detail");
        await ApproveBookingAsync(bookingId);

        using var client = CreateClient();
        await IdentityHelper.LoginViaUiAsync(client, "requester@newvenue.test", SeedPassword);

        var workspacePage = await client.GetAsync("/workspace");
        workspacePage.EnsureSuccessStatusCode();

        var document = await HtmlHelpers.GetDocumentAsync(workspacePage);
        var detailLink = document.QuerySelectorAll("a.action-link").OfType<IHtmlAnchorElement>()
            .First(item => item.PathName.EndsWith($"/workspace/bookings/{bookingId}"));

        var detailsResponse = await client.GetAsync(detailLink.Href);
        detailsResponse.EnsureSuccessStatusCode();

        var markup = await detailsResponse.Content.ReadAsStringAsync();
        Assert.Contains("Requester Booking Detail", markup);
        Assert.Contains("Confirmed", markup);
        Assert.Contains("Due", markup);
        Assert.Contains("Catering change deadline", markup);
    }

    [Fact]
    public async Task ManagerCanCreateSpaceWithinActiveVenue()
    {
        await SetManagerActiveVenueAsync("northstar-conference-center");
        using var client = CreateClient();
        await IdentityHelper.LoginViaUiAsync(client, "manager@northstarvenues.test", SeedPassword);

        var beforeCount = await CountSpacesAsync("northstar-conference-center");

        var spacesPage = await client.GetAsync("/Admin/Spaces?create=true");
        spacesPage.EnsureSuccessStatusCode();

        var document = await HtmlHelpers.GetDocumentAsync(spacesPage);
        var form = Assert.Single(
            document.QuerySelectorAll("form").OfType<IHtmlFormElement>(),
            item => item.Action.EndsWith("/Admin/Spaces/Save"));

        var postResponse = await client.SendAsync(
            form,
            new Dictionary<string, string>
            {
                ["Form.Name"] = "Skyline Studio",
                ["Form.Code"] = "SKY",
                ["Form.Status"] = "Draft",
                ["Form.Description"] = "Flexible studio for breakouts and hybrid calls.",
                ["Form.MinimumBookingDurationMinutes"] = "120",
                ["Form.HourlyRateAmount"] = "175",
                ["Form.Currency"] = "EUR",
                ["Form.MinimumCapacity"] = "10",
                ["Form.RecommendedCapacity"] = "28",
                ["Form.MaximumCapacity"] = "40",
                ["Form.Layouts[0].Name"] = "Workshop",
                ["Form.Layouts[0].LayoutType"] = "Classroom",
                ["Form.Layouts[0].Capacity"] = "28",
                ["Form.Layouts[0].IsDefault"] = "true",
                ["Form.Layouts[0].Notes"] = "Movable tables and wall display."
            });

        Assert.Equal(HttpStatusCode.Redirect, postResponse.StatusCode);

        var afterCount = await CountSpacesAsync("northstar-conference-center");
        Assert.Equal(beforeCount + 1, afterCount);

        var createdSpace = await GetSpaceByCodeAsync("northstar-conference-center", "SKY");
        Assert.Equal("Skyline Studio", createdSpace.Name);
        Assert.Equal(SpaceStatus.Draft, createdSpace.Status);
    }

    [Fact]
    public async Task EmployeeBookingsPage_ShowsVenueBookingCalendar()
    {
        await SetManagerActiveVenueAsync("northstar-conference-center");
        await CreatePendingBookingRequestAsync();

        using var client = CreateClient();
        await IdentityHelper.LoginViaUiAsync(client, "employee@northstarvenues.test", SeedPassword);

        var response = await client.GetAsync("/Employee/Bookings");
        response.EnsureSuccessStatusCode();

        var markup = await response.Content.ReadAsStringAsync();
        Assert.Contains("Venue booking calendar", markup);
        Assert.Contains("Duration", markup);
        Assert.Contains("Pending Approval Event", markup);
    }

    [Fact]
    public async Task ManagerCanSwitchVenueContextFromWorkspace()
    {
        await SetManagerSecondaryVenueAccessLevelAsync(VenueAccessLevel.Manager);

        using var client = CreateClient();
        await IdentityHelper.LoginViaUiAsync(client, "manager@northstarvenues.test", SeedPassword);

        var venue = await GetVenueBySlugAsync("harbor-hall");
        var dashboardPage = await client.GetAsync("/Admin/Dashboard");
        dashboardPage.EnsureSuccessStatusCode();

        var document = await HtmlHelpers.GetDocumentAsync(dashboardPage);
        var form = Assert.Single(
            document.QuerySelectorAll("form").OfType<IHtmlFormElement>(),
            item => item.Action.EndsWith("/workspace/active-venue"));

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
    public async Task ManagerSwitchingToEmployeeOnlyVenue_RedirectsToEmployeeDashboard()
    {
        await SetManagerSecondaryVenueAccessLevelAsync(VenueAccessLevel.Employee);

        using var client = CreateClient();
        await IdentityHelper.LoginViaUiAsync(client, "manager@northstarvenues.test", SeedPassword);

        var venue = await GetVenueBySlugAsync("harbor-hall");
        var dashboardPage = await client.GetAsync("/Admin/Dashboard");
        dashboardPage.EnsureSuccessStatusCode();

        var document = await HtmlHelpers.GetDocumentAsync(dashboardPage);
        var form = Assert.Single(
            document.QuerySelectorAll("form").OfType<IHtmlFormElement>(),
            item => item.Action.EndsWith("/workspace/active-venue"));

        var switchResponse = await client.SendAsync(
            form,
            new Dictionary<string, string>
            {
                ["venueId"] = venue.Id.ToString()
            });

        Assert.Equal(HttpStatusCode.Redirect, switchResponse.StatusCode);
        Assert.Equal("/Employee/Dashboard", switchResponse.Headers.Location?.ToString());
    }

    [Fact]
    public async Task AdminDashboard_WithEmployeeOnlyActiveVenue_RedirectsToEmployeeDashboard()
    {
        await SetManagerSecondaryVenueAccessLevelAsync(VenueAccessLevel.Employee);
        await SetManagerActiveVenueAsync("harbor-hall");

        using var client = CreateClient();
        await IdentityHelper.LoginViaUiAsync(client, "manager@northstarvenues.test", SeedPassword);

        var response = await client.GetAsync("/Admin/Dashboard");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Employee/Dashboard", response.Headers.Location?.ToString());
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

    [Fact]
    public async Task AdminCanReviewVenueAccessRequestFromRequestsPage()
    {
        using var adminClient = CreateClient();
        await IdentityHelper.LoginViaUiAsync(adminClient, "akaver@akaver.com", SeedPassword);

        var pendingRequestId = await GetFirstVenueRequestIdAsync();

        var requestPage = await adminClient.GetAsync($"/Admin/Requests?requestId={pendingRequestId}");
        requestPage.EnsureSuccessStatusCode();

        var document = await HtmlHelpers.GetDocumentAsync(requestPage);
        var form = Assert.Single(
            document.QuerySelectorAll("form").OfType<IHtmlFormElement>(),
            item => item.Action.EndsWith("/Admin/Requests/Review"));

        var postResponse = await adminClient.SendAsync(
            form,
            new Dictionary<string, string>
            {
                ["ReviewForm.Status"] = "Approved",
                ["ReviewForm.ApprovedAccessLevel"] = "Manager",
                ["ReviewForm.ReviewNotes"] = "Approved during integration test."
            });

        Assert.Equal(HttpStatusCode.Redirect, postResponse.StatusCode);
        Assert.Equal($"/Admin/Requests?requestId={pendingRequestId}", postResponse.Headers.Location?.ToString());

        var reviewedRequest = await GetVenueRequestAsync(pendingRequestId);
        Assert.Equal("Approved", reviewedRequest.Status.ToString());
    }

    [Fact]
    public async Task AdminApprovalWithMembershipAssignment_GrantsWorkspaceAccessToRequester()
    {
        var requestId = await CreatePendingMembershipAssignmentRequestAsync();

        using var adminClient = CreateClient();
        await IdentityHelper.LoginViaUiAsync(adminClient, "akaver@akaver.com", SeedPassword);

        var requestPage = await adminClient.GetAsync($"/Admin/Requests?requestId={requestId}");
        requestPage.EnsureSuccessStatusCode();

        var document = await HtmlHelpers.GetDocumentAsync(requestPage);
        var form = Assert.Single(
            document.QuerySelectorAll("form").OfType<IHtmlFormElement>(),
            item => item.Action.EndsWith("/Admin/Requests/Review"));

        var approveResponse = await adminClient.SendAsync(
            form,
            new Dictionary<string, string>
            {
                ["ReviewForm.RequestId"] = requestId.ToString(),
                ["ReviewForm.Status"] = "Approved",
                ["ReviewForm.ReviewNotes"] = "Approved during integration test."
            });

        Assert.Equal(HttpStatusCode.Redirect, approveResponse.StatusCode);
        Assert.Equal($"/Admin/Requests?requestId={requestId}", approveResponse.Headers.Location?.ToString());

        var approvedRequestPage = await adminClient.GetAsync($"/Admin/Requests?requestId={requestId}");
        approvedRequestPage.EnsureSuccessStatusCode();

        var approvedDocument = await HtmlHelpers.GetDocumentAsync(approvedRequestPage);
        var approvedForm = Assert.Single(
            approvedDocument.QuerySelectorAll("form").OfType<IHtmlFormElement>(),
            item => item.Action.EndsWith("/Admin/Requests/Review"));

        var assignResponse = await adminClient.SendAsync(
            approvedForm,
            new Dictionary<string, string>
            {
                ["ReviewForm.RequestId"] = requestId.ToString(),
                ["ReviewForm.Status"] = "Approved",
                ["ReviewForm.ApprovedAccessLevel"] = "Manager",
                ["ReviewForm.ReviewNotes"] = "Approved and assigned during integration test.",
                ["ReviewForm.AssignMembership"] = bool.TrueString
            });

        Assert.Equal(HttpStatusCode.Redirect, assignResponse.StatusCode);
        Assert.Equal($"/Admin/Requests?requestId={requestId}", assignResponse.Headers.Location?.ToString());

        using var requesterClient = CreateClient();
        await IdentityHelper.LoginViaUiAsync(requesterClient, "requester@newvenue.test", SeedPassword);

        var workspaceResponse = await requesterClient.GetAsync("/workspace");

        Assert.Equal(HttpStatusCode.Redirect, workspaceResponse.StatusCode);
        Assert.Equal("/Admin/Dashboard", workspaceResponse.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Workspace_ShowsApprovedRequestStatusToRequesterWithoutMembership()
    {
        await ResetRequesterOperatorAccessAsync();
        var requestId = await CreatePendingMembershipAssignmentRequestAsync();

        using var adminClient = CreateClient();
        await IdentityHelper.LoginViaUiAsync(adminClient, "akaver@akaver.com", SeedPassword);

        var requestPage = await adminClient.GetAsync($"/Admin/Requests?requestId={requestId}");
        requestPage.EnsureSuccessStatusCode();

        var document = await HtmlHelpers.GetDocumentAsync(requestPage);
        var form = Assert.Single(
            document.QuerySelectorAll("form").OfType<IHtmlFormElement>(),
            item => item.Action.EndsWith("/Admin/Requests/Review"));

        var approveResponse = await adminClient.SendAsync(
            form,
            new Dictionary<string, string>
            {
                ["ReviewForm.RequestId"] = requestId.ToString(),
                ["ReviewForm.Status"] = "Approved",
                ["ReviewForm.ReviewNotes"] = "Approved without membership assignment."
            });

        Assert.Equal(HttpStatusCode.Redirect, approveResponse.StatusCode);
        Assert.Equal($"/Admin/Requests?requestId={requestId}", approveResponse.Headers.Location?.ToString());

        using var requesterClient = CreateClient();
        await IdentityHelper.LoginViaUiAsync(requesterClient, "requester@newvenue.test", SeedPassword);

        var workspacePage = await requesterClient.GetAsync("/workspace");
        workspacePage.EnsureSuccessStatusCode();

        var markup = await workspacePage.Content.ReadAsStringAsync();
        Assert.Contains("Harbor Hall", markup);
        Assert.Contains("Approved", markup);
        Assert.Contains("assign venue membership", markup);
    }

    [Fact]
    public async Task Workspace_ShowsPendingBookingRequestsToRequester()
    {
        await ResetRequesterOperatorAccessAsync();
        await CreatePendingBookingRequestAsync();

        using var requesterClient = CreateClient();
        await IdentityHelper.LoginViaUiAsync(requesterClient, "requester@newvenue.test", SeedPassword);

        var workspacePage = await requesterClient.GetAsync("/workspace");
        workspacePage.EnsureSuccessStatusCode();

        var markup = await workspacePage.Content.ReadAsStringAsync();
        Assert.Contains("My bookings", markup);
        Assert.Contains("Your booking requests", markup);
        Assert.Contains("Your booking calendar", markup);
        Assert.Contains("Pending Approval Event", markup);
        Assert.Contains("Waiting for a venue employee or manager to approve this booking request.", markup);
        Assert.Contains("Duration", markup);
    }

    [Fact]
    public async Task WorkspaceBookingsPage_ShowsRegularUserBookingCalendar()
    {
        await ResetRequesterOperatorAccessAsync();
        await CreatePendingBookingRequestAsync();

        using var requesterClient = CreateClient();
        await IdentityHelper.LoginViaUiAsync(requesterClient, "requester@newvenue.test", SeedPassword);

        var bookingsPage = await requesterClient.GetAsync("/workspace/bookings");
        bookingsPage.EnsureSuccessStatusCode();

        var markup = await bookingsPage.Content.ReadAsStringAsync();
        Assert.Contains("Your bookings", markup);
        Assert.Contains("See every venue booking you requested in one calendar view.", markup);
        Assert.Contains("Your booking calendar", markup);
        Assert.Contains("Pending Approval Event", markup);
    }

    [Fact]
    public async Task AuthenticatedRegularUser_SeesMyBookingsButtonInPublicNavigation()
    {
        await ResetRequesterOperatorAccessAsync();
        await CreatePendingBookingRequestAsync();

        using var requesterClient = CreateClient();
        await IdentityHelper.LoginViaUiAsync(requesterClient, "requester@newvenue.test", SeedPassword);

        var response = await requesterClient.GetAsync("/");
        response.EnsureSuccessStatusCode();

        var markup = await response.Content.ReadAsStringAsync();
        Assert.Contains("href=\"/workspace/bookings\"", markup);
    }

    [Fact]
    public async Task Workspace_ShowsApprovedBookingAsScheduledAfterManagerApproval()
    {
        await ResetRequesterOperatorAccessAsync();
        await SetManagerActiveVenueAsync("northstar-conference-center");
        var bookingId = await CreatePendingBookingRequestAsync();

        using var managerClient = CreateClient();
        await IdentityHelper.LoginViaUiAsync(managerClient, "manager@northstarvenues.test", SeedPassword);

        var approvalPage = await managerClient.GetAsync($"/Employee/Coordination?bookingId={bookingId}");
        approvalPage.EnsureSuccessStatusCode();

        var document = await HtmlHelpers.GetDocumentAsync(approvalPage);
        var form = Assert.Single(
            document.QuerySelectorAll("form").OfType<IHtmlFormElement>(),
            item => item.Action.EndsWith("/Employee/Coordination/Approve"));

        var approveResponse = await managerClient.SendAsync(
            form,
            new Dictionary<string, string>
            {
                ["bookingId"] = bookingId.ToString()
            });

        Assert.Equal(HttpStatusCode.Redirect, approveResponse.StatusCode);

        using var requesterClient = CreateClient();
        await IdentityHelper.LoginViaUiAsync(requesterClient, "requester@newvenue.test", SeedPassword);

        var workspacePage = await requesterClient.GetAsync("/workspace");
        workspacePage.EnsureSuccessStatusCode();

        var markup = await workspacePage.Content.ReadAsStringAsync();
        Assert.Contains("Pending Approval Event", markup);
        Assert.Contains("Confirmed", markup);
        Assert.Contains("Approved and scheduled. This booking is now part of the venue calendar.", markup);
        Assert.Contains("Your booking calendar", markup);
    }

    [Fact]
    public async Task Workspace_ShowsAccessReadyMessageWhenMembershipExistsButSessionIsStale()
    {
        await ResetRequesterOperatorAccessAsync();
        var requestId = await CreatePendingMembershipAssignmentRequestAsync();

        using var requesterClient = CreateClient();
        await IdentityHelper.LoginViaUiAsync(requesterClient, "requester@newvenue.test", SeedPassword);

        using var adminClient = CreateClient();
        await IdentityHelper.LoginViaUiAsync(adminClient, "akaver@akaver.com", SeedPassword);

        var requestPage = await adminClient.GetAsync($"/Admin/Requests?requestId={requestId}");
        requestPage.EnsureSuccessStatusCode();

        var document = await HtmlHelpers.GetDocumentAsync(requestPage);
        var form = Assert.Single(
            document.QuerySelectorAll("form").OfType<IHtmlFormElement>(),
            item => item.Action.EndsWith("/Admin/Requests/Review"));

        var assignResponse = await adminClient.SendAsync(
            form,
            new Dictionary<string, string>
            {
                ["ReviewForm.RequestId"] = requestId.ToString(),
                ["ReviewForm.Status"] = "Approved",
                ["ReviewForm.ApprovedAccessLevel"] = "Manager",
                ["ReviewForm.ReviewNotes"] = "Approved and assigned during integration test.",
                ["ReviewForm.AssignMembership"] = bool.TrueString
            });

        Assert.Equal(HttpStatusCode.Redirect, assignResponse.StatusCode);

        var workspacePage = await requesterClient.GetAsync("/workspace");
        workspacePage.EnsureSuccessStatusCode();

        var markup = await workspacePage.Content.ReadAsStringAsync();
        Assert.Contains("Harbor Hall", markup);
        Assert.Contains("Approved", markup);
    }

    [Fact]
    public async Task AdminRequests_ShowSelectedRequestCreatorAndSubmittedFields()
    {
        using var adminClient = CreateClient();
        await IdentityHelper.LoginViaUiAsync(adminClient, "akaver@akaver.com", SeedPassword);

        var requestId = await GetVenueRequestIdByVenueNameAsync("Lighthouse Forum");

        var requestPage = await adminClient.GetAsync($"/Admin/Requests?requestId={requestId}");
        requestPage.EnsureSuccessStatusCode();

        var document = await HtmlHelpers.GetDocumentAsync(requestPage);
        var pageText = document.DocumentElement.TextContent;

        Assert.Contains("requester@newvenue.test", pageText);
        Assert.Contains("Marta Saar", pageText);
        Assert.Contains("marta@lighthouseevents.test", pageText);
        Assert.Contains("+3725550199", pageText);
        Assert.Contains("Pärnu", pageText);
        Assert.Contains("Ringi 8", pageText);
        Assert.Contains("Interested in onboarding one flagship venue first.", pageText);
        Assert.Contains("12", pageText);
    }

    [Fact]
    public async Task AdminCanApproveInitialVenueApplicationWithoutAssigningMembership()
    {
        using var adminClient = CreateClient();
        await IdentityHelper.LoginViaUiAsync(adminClient, "akaver@akaver.com", SeedPassword);

        var onboardingRequestId = await GetVenueRequestIdByVenueNameAsync("Lighthouse Forum");

        var requestPage = await adminClient.GetAsync($"/Admin/Requests?requestId={onboardingRequestId}");
        requestPage.EnsureSuccessStatusCode();
        var markup = await requestPage.Content.ReadAsStringAsync();
        Assert.Contains("initial venue onboarding application", markup);
        Assert.DoesNotContain("ReviewForm.AssignMembership", markup);

        var document = await HtmlHelpers.GetDocumentAsync(requestPage);
        var form = Assert.Single(
            document.QuerySelectorAll("form").OfType<IHtmlFormElement>(),
            item => item.Action.EndsWith("/Admin/Requests/Review"));

        var postResponse = await adminClient.SendAsync(
            form,
            new Dictionary<string, string>
            {
                ["ReviewForm.Status"] = "Approved",
                ["ReviewForm.ReviewNotes"] = "Approved for venue creation."
            });

        Assert.Equal(HttpStatusCode.Redirect, postResponse.StatusCode);
        Assert.Equal($"/Admin/Requests?requestId={onboardingRequestId}", postResponse.Headers.Location?.ToString());

        var reviewedRequest = await GetVenueRequestAsync(onboardingRequestId);
        Assert.Equal("Approved", reviewedRequest.Status.ToString());
        Assert.NotNull(reviewedRequest.CompanyId);
        Assert.NotNull(reviewedRequest.VenueId);

        var linkedVenue = await GetVenueAsync(reviewedRequest.VenueId!.Value);
        Assert.Equal("Lighthouse Forum", linkedVenue.Name);

        var refreshedPage = await adminClient.GetAsync($"/Admin/Requests?requestId={onboardingRequestId}");
        refreshedPage.EnsureSuccessStatusCode();
        var refreshedMarkup = await refreshedPage.Content.ReadAsStringAsync();
        Assert.Contains("Approved", refreshedMarkup);
    }

    [Fact]
    public async Task AdminCanArchiveRejectedVenueFromRequestsPage()
    {
        var requestId = await CreateRejectedVenueRequestAsync();

        using var adminClient = CreateClient();
        await IdentityHelper.LoginViaUiAsync(adminClient, "akaver@akaver.com", SeedPassword);

        var requestPage = await adminClient.GetAsync($"/Admin/Requests?requestId={requestId}");
        requestPage.EnsureSuccessStatusCode();
        var markup = await requestPage.Content.ReadAsStringAsync();
        Assert.Contains("Archive rejected venue", markup);

        var document = await HtmlHelpers.GetDocumentAsync(requestPage);
        var archiveForm = Assert.Single(
            document.QuerySelectorAll("form").OfType<IHtmlFormElement>(),
            item => item.Action.EndsWith("/Admin/Requests/ArchiveRejectedVenue"));

        var postResponse = await adminClient.SendAsync(
            archiveForm,
            new Dictionary<string, string>
            {
                ["requestId"] = requestId.ToString()
            });

        Assert.Equal(HttpStatusCode.Redirect, postResponse.StatusCode);
        Assert.Equal("/Admin/Requests", postResponse.Headers.Location?.ToString());
        Assert.True(await VenueRequestExistsAsync(requestId));
        var archivedRequest = await GetVenueRequestAsync(requestId);
        var archivedVenue = await GetVenueAsync(archivedRequest.VenueId!.Value);
        Assert.Equal(VenueLifecycleStatus.Archived, archivedVenue.Status);
    }

    [Fact]
    public async Task AdminCanArchiveRejectedVenueFromRequestsPage_WhenMembershipExists()
    {
        var requestId = await CreateRejectedVenueRequestWithMembershipAsync(VenueAccessLevel.Manager);

        using var adminClient = CreateClient();
        await IdentityHelper.LoginViaUiAsync(adminClient, "akaver@akaver.com", SeedPassword);

        var requestPage = await adminClient.GetAsync($"/Admin/Requests?requestId={requestId}");
        requestPage.EnsureSuccessStatusCode();

        var document = await HtmlHelpers.GetDocumentAsync(requestPage);
        var archiveForm = Assert.Single(
            document.QuerySelectorAll("form").OfType<IHtmlFormElement>(),
            item => item.Action.EndsWith("/Admin/Requests/ArchiveRejectedVenue"));

        var postResponse = await adminClient.SendAsync(
            archiveForm,
            new Dictionary<string, string>
            {
                ["requestId"] = requestId.ToString()
            });

        Assert.Equal(HttpStatusCode.Redirect, postResponse.StatusCode);
        Assert.Equal("/Admin/Requests", postResponse.Headers.Location?.ToString());
        Assert.True(await VenueRequestExistsAsync(requestId));
    }

    [Fact]
    public async Task EmployeeDashboard_ShowsRejectedVenueNoticeForArchivedVenue()
    {
        await ResetRequesterOperatorAccessAsync();
        var requestId = await CreateRejectedVenueRequestWithMembershipAsync(
            VenueAccessLevel.Employee,
            "Rejected after employee review.");

        using var employeeClient = CreateClient();
        await IdentityHelper.LoginViaUiAsync(employeeClient, "requester@newvenue.test", SeedPassword);

        var response = await employeeClient.GetAsync("/Employee/Dashboard");
        response.EnsureSuccessStatusCode();
        var markup = await response.Content.ReadAsStringAsync();

        Assert.Contains("This venue has been rejected", markup);
        Assert.Contains("Rejected after employee review.", markup);
        Assert.True(await VenueRequestExistsAsync(requestId));
    }

    [Fact]
    public async Task ManagerDashboard_ShowsRejectedVenueNoticeForArchivedVenue()
    {
        await ResetRequesterOperatorAccessAsync();
        var requestId = await CreateRejectedVenueRequestWithMembershipAsync(
            VenueAccessLevel.Manager,
            "Rejected after manager review.");

        using var managerClient = CreateClient();
        await IdentityHelper.LoginViaUiAsync(managerClient, "requester@newvenue.test", SeedPassword);

        var response = await managerClient.GetAsync("/Admin/Dashboard");
        response.EnsureSuccessStatusCode();
        var markup = await response.Content.ReadAsStringAsync();

        Assert.Contains("This venue has been rejected", markup);
        Assert.Contains("Rejected after manager review.", markup);
        Assert.True(await VenueRequestExistsAsync(requestId));
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

    private async Task<int> CountSpacesAsync(string venueSlug)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Spaces.CountAsync(item => item.Venue.Slug == venueSlug);
    }

    private async Task<int> CountBookingsAsync(string venueSlug)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Bookings.CountAsync(item => item.Venue.Slug == venueSlug);
    }

    private async Task<Space> GetSpaceByCodeAsync(string venueSlug, string code)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Spaces
            .Include(item => item.Venue)
            .SingleAsync(item => item.Venue.Slug == venueSlug && item.Code == code);
    }

    private async Task<Guid> GetSpaceIdByCodeAsync(string venueSlug, string code)
    {
        var space = await GetSpaceByCodeAsync(venueSlug, code);
        return space.Id;
    }

    private async Task<Booking> GetBookingByTitleAsync(string title)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Bookings
            .Include(item => item.CateringOrders)
            .SingleAsync(item => item.Title == title);
    }

    private async Task<Booking> GetBookingAsync(Guid bookingId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Bookings.SingleAsync(item => item.Id == bookingId);
    }

    private async Task<Guid> GetFirstVenueRequestIdAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await Task.FromResult(db.VenueAccessRequests.OrderBy(request => request.SubmittedAt).Select(request => request.Id).First());
    }

    private async Task<App.Domain.Venues.VenueAccessRequest> GetVenueRequestAsync(Guid requestId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await Task.FromResult(db.VenueAccessRequests.Single(request => request.Id == requestId));
    }

    private async Task<Guid> GetVenueRequestIdByVenueNameAsync(string venueName)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await Task.FromResult(
            db.VenueAccessRequests
                .Where(request => request.VenueName == venueName)
                .Select(request => request.Id)
                .Single());
    }

    private async Task<App.Domain.Venues.Venue> GetVenueAsync(Guid venueId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await Task.FromResult(db.Venues.Single(venue => venue.Id == venueId));
    }

    private async Task<bool> VenueRequestExistsAsync(Guid requestId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.VenueAccessRequests.AnyAsync(item => item.Id == requestId);
    }

    private async Task<Guid> CreatePendingMembershipAssignmentRequestAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var requester = await db.Users.SingleAsync(user => user.Email == "requester@newvenue.test");
        var venue = await db.Venues
            .Include(item => item.Company)
            .SingleAsync(item => item.Slug == "harbor-hall");

        var request = new VenueAccessRequest
        {
            RequestorUserId = requester.Id,
            CompanyId = venue.CompanyId,
            VenueId = venue.Id,
            CompanyName = venue.Company.Name,
            VenueName = venue.Name,
            ContactName = "Requester User",
            ContactEmail = requester.Email!,
            City = venue.City,
            Country = venue.Country,
            AddressLine1 = venue.AddressLine1,
            EstimatedMonthlyBookings = 6,
            Status = VenueAccessRequestStatus.PendingReview
        };

        db.VenueAccessRequests.Add(request);
        await db.SaveChangesAsync();

        return request.Id;
    }

    private async Task<Guid> CreatePendingBookingRequestAsync(string title = "Pending Approval Event")
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var requester = await db.Users.SingleAsync(user => user.Email == "requester@newvenue.test");
        var venue = await db.Venues.SingleAsync(item => item.Slug == "northstar-conference-center");
        var space = await db.Spaces.SingleAsync(item => item.VenueId == venue.Id && item.Code == "AUR");
        var bookingIndex = await db.Bookings.CountAsync(item => item.VenueId == venue.Id && item.SpaceId == space.Id);
        var startsAt = DateTime.UtcNow.Date.AddDays(18 + bookingIndex).AddHours(9);

        var booking = new Booking
        {
            VenueId = venue.Id,
            SpaceId = space.Id,
            CreatedByUserId = requester.Id,
            Title = title,
            ClientName = "Requester Co",
            Status = BookingStatus.PendingApproval,
            Schedule = new ScheduleWindow(startsAt, startsAt.AddHours(5)),
            ExpectedAttendees = 50,
            SpaceCharge = new App.Domain.ValueObjects.Money(1100m, "EUR"),
            CoordinationNotes = "Requested layout: Theater 180 (Theater)"
        };

        db.Bookings.Add(booking);
        await db.SaveChangesAsync();

        return booking.Id;
    }

    private async Task ApproveBookingAsync(Guid bookingId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var booking = await db.Bookings.SingleAsync(item => item.Id == bookingId);
        booking.Status = BookingStatus.Confirmed;
        await db.SaveChangesAsync();
    }

    private async Task<Guid> CreateRejectedVenueRequestAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var requester = await db.Users.SingleAsync(user => user.Email == "requester@newvenue.test");

        var company = new Company
        {
            Name = "Rejected Venue Group",
            RegistrationCode = "RVG-001",
            ContactEmail = "ops@rejectedvenue.test"
        };

        var venue = new Venue
        {
            Company = company,
            Name = "Rejected Venue",
            Slug = $"rejected-venue-{Guid.NewGuid():N}",
            City = "Tallinn",
            Country = "Estonia",
            AddressLine1 = "Sadama 7",
            Status = VenueLifecycleStatus.Active
        };

        var request = new VenueAccessRequest
        {
            RequestorUserId = requester.Id,
            Company = company,
            Venue = venue,
            CompanyName = company.Name,
            VenueName = venue.Name,
            ContactName = "Requester User",
            ContactEmail = requester.Email!,
            City = venue.City,
            Country = venue.Country,
            AddressLine1 = venue.AddressLine1,
            EstimatedMonthlyBookings = 2,
            Status = VenueAccessRequestStatus.Rejected,
            ReviewNotes = "Rejected during integration setup."
        };

        db.AddRange(company, venue, request);
        await db.SaveChangesAsync();

        return request.Id;
    }

    private async Task<Guid> CreateRejectedVenueRequestWithMembershipAsync(
        VenueAccessLevel accessLevel,
        string reviewNotes = "Rejected during integration setup.")
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        var requester = await db.Users.SingleAsync(user => user.Email == "requester@newvenue.test");

        var company = new Company
        {
            Name = "Rejected Venue Group Membership",
            RegistrationCode = $"RVG-{Guid.NewGuid():N}"[..12],
            ContactEmail = "ops-membership@rejectedvenue.test"
        };

        var venue = new Venue
        {
            Company = company,
            Name = "Rejected Venue Membership",
            Slug = $"rejected-venue-membership-{Guid.NewGuid():N}",
            City = "Tallinn",
            Country = "Estonia",
            AddressLine1 = "Sadama 9",
            Status = VenueLifecycleStatus.Active
        };

        var request = new VenueAccessRequest
        {
            RequestorUserId = requester.Id,
            Company = company,
            Venue = venue,
            CompanyName = company.Name,
            VenueName = venue.Name,
            ContactName = "Requester User",
            ContactEmail = requester.Email!,
            City = venue.City,
            Country = venue.Country,
            AddressLine1 = venue.AddressLine1,
            EstimatedMonthlyBookings = 2,
            Status = VenueAccessRequestStatus.Rejected,
            ReviewNotes = reviewNotes
        };

        var membership = new VenueMembership
        {
            Company = company,
            Venue = venue,
            UserId = requester.Id,
            AccessLevel = accessLevel,
            Status = VenueMembershipStatus.Active,
            IsDefaultVenue = true
        };

        requester.ActiveVenueId = venue.Id;
        venue.Status = VenueLifecycleStatus.Archived;

        db.AddRange(company, venue, request, membership);
        await db.SaveChangesAsync();

        if (accessLevel == VenueAccessLevel.Manager)
        {
            await userManager.AddToRoleAsync(requester, AppRoles.CompanyManager);
        }
        else
        {
            await userManager.AddToRoleAsync(requester, AppRoles.CompanyEmployee);
        }

        return request.Id;
    }

    private async Task ResetRequesterOperatorAccessAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        var requester = await db.Users.SingleAsync(user => user.Email == "requester@newvenue.test");
        requester.ActiveVenueId = null;

        var memberships = await db.VenueMemberships
            .Where(item => item.UserId == requester.Id)
            .ToListAsync();
        db.VenueMemberships.RemoveRange(memberships);

        await db.SaveChangesAsync();

        if (await userManager.IsInRoleAsync(requester, AppRoles.CompanyManager))
        {
            await userManager.RemoveFromRoleAsync(requester, AppRoles.CompanyManager);
        }

        if (await userManager.IsInRoleAsync(requester, AppRoles.CompanyEmployee))
        {
            await userManager.RemoveFromRoleAsync(requester, AppRoles.CompanyEmployee);
        }
    }

    private async Task SetManagerSecondaryVenueAccessLevelAsync(VenueAccessLevel accessLevel)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var manager = await db.Users.SingleAsync(user => user.Email == "manager@northstarvenues.test");
        var harborVenue = await db.Venues.SingleAsync(venue => venue.Slug == "harbor-hall");
        var membership = await db.VenueMemberships.SingleAsync(item =>
            item.UserId == manager.Id &&
            item.VenueId == harborVenue.Id);

        membership.AccessLevel = accessLevel;
        await db.SaveChangesAsync();
    }

    private async Task SetManagerActiveVenueAsync(string venueSlug)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var manager = await db.Users.SingleAsync(user => user.Email == "manager@northstarvenues.test");
        var venue = await db.Venues.SingleAsync(item => item.Slug == venueSlug);
        manager.ActiveVenueId = venue.Id;
        await db.SaveChangesAsync();
    }
}

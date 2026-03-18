using System;
using System.Threading.Tasks;
using App.BLL.Services;
using App.DTO.v1.Venues.Public;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WebApp.Controllers;
using Xunit;
using Xunit.Abstractions;

namespace WebApp.Tests.Unit;

public class UnitTestHomeController
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly HomeController _homeController;

    public UnitTestHomeController(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;

        // set up logger - it is not mocked, so we are not testing logging functionality
        using var logFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = logFactory.CreateLogger<HomeController>();

        _homeController = new HomeController(new FakePublicVenueDiscoveryService(), logger);
    }
    
    [Fact]
    public async Task IndexAction_ReturnsLandingVm()
    {
        var result = (await _homeController.Index(default)) as ViewResult;
        _testOutputHelper.WriteLine(result?.ToString());
        Assert.NotNull(result);
        Assert.NotNull(result!.Model);
    }
}

file sealed class FakePublicVenueDiscoveryService : IPublicVenueDiscoveryService
{
    public Task<PublicLandingPageDto> GetLandingPageAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(new PublicLandingPageDto());

    public Task<IReadOnlyList<PublicVenueSummaryDto>> GetBrowseVenuesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<PublicVenueSummaryDto>>([]);

    public Task<PublicVenueDetailDto?> GetVenueAsync(string slug, CancellationToken cancellationToken = default) =>
        Task.FromResult<PublicVenueDetailDto?>(null);

    public Task<IReadOnlyList<UserVenueAccessRequestSummaryDto>> GetUserVenueAccessRequestsAsync(
        Guid requestorUserId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<UserVenueAccessRequestSummaryDto>>([]);

    public Task<VenueAccessRequestSubmissionResultDto> SubmitVenueAccessRequestAsync(
        Guid requestorUserId,
        SubmitVenueAccessRequestDto dto,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}

using App.BLL.Services;
using App.DAL.EF;
using App.Domain.Identity;
using App.Domain.ValueObjects;
using App.Domain.Venues;
using App.DTO.v1.Venues.Admin;
using App.DTO.v1.Venues.Employee;
using App.DTO.v1.Venues.Membership;
using App.DTO.v1.Venues.Public;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace WebApp.Tests.Unit;

public class VenuePlatformServicesTests
{
    [Fact]
    public async Task SubmitVenueAccessRequestAsync_CreatesPendingRequest()
    {
        await using var context = CreateContext();
        var fixture = await SeedVenueFixtureAsync(context);
        var service = new PublicVenueDiscoveryService(context);

        var result = await service.SubmitVenueAccessRequestAsync(
            fixture.Requester.Id,
            new SubmitVenueAccessRequestDto
            {
                CompanyName = "Lighthouse Events",
                VenueName = "Lighthouse Forum",
                ContactName = "Marta Saar",
                ContactEmail = "marta@lighthouseevents.test",
                City = "Parnu",
                Country = "Estonia",
                AddressLine1 = "Ringi 8",
                EstimatedMonthlyBookings = 12
            });

        var request = await context.VenueAccessRequests.SingleAsync(item => item.Id == result.RequestId);
        Assert.Equal(VenueAccessRequestStatus.PendingReview, request.Status);
        Assert.Equal(fixture.Requester.Id, request.RequestorUserId);
    }

    [Fact]
    public async Task SubmitBookingRequestAsync_CreatesPendingApprovalBookingAndDraftCatering()
    {
        await using var context = CreateContext();
        var fixture = await SeedVenueFixtureAsync(context);
        var service = new PublicVenueDiscoveryService(context);

        var startsAt = DateTime.UtcNow.AddDays(10).Date.AddHours(9);
        var endsAt = startsAt.AddHours(6);

        var result = await service.SubmitBookingRequestAsync(
            fixture.Requester.Id,
            fixture.PrimaryVenue.Slug,
            new SubmitPublicBookingRequestDto
            {
                SpaceId = fixture.Space.Id,
                LayoutId = fixture.Layout.Id,
                EventTitle = "Product Launch",
                ClientName = "Fjord Labs",
                StartsAt = startsAt,
                EndsAt = endsAt,
                ExpectedAttendees = 80,
                CateringNotes = "Coffee station and lunch buffet.",
                SetupRequirements = "Stage, handheld microphones, front registration desk.",
                AdditionalRequirements = "Wheelchair-friendly front row seating."
            });

        var booking = await context.Bookings
            .Include(item => item.CateringOrders)
            .SingleAsync(item => item.Id == result.BookingId);

        Assert.Equal(BookingStatus.PendingApproval, booking.Status);
        Assert.Equal(fixture.Requester.Id, booking.CreatedByUserId);
        Assert.Contains("Requested layout", booking.CoordinationNotes);
        Assert.Contains("Setup requirements", booking.CoordinationNotes);
        Assert.Single(booking.CateringOrders);
        Assert.Equal(CateringOrderStatus.Draft, booking.CateringOrders.Single().Status);
        Assert.Contains("Coffee station and lunch buffet.", booking.CateringOrders.Single().Notes);
    }

    [Fact]
    public async Task GetBrowseVenuesAsync_FiltersByCityAndMinimumCapacity()
    {
        await using var context = CreateContext();
        var fixture = await SeedVenueFixtureAsync(context);
        fixture.SecondaryVenue.City = "Riga";
        await context.SaveChangesAsync();

        var service = new PublicVenueDiscoveryService(context);

        var result = await service.GetBrowseVenuesAsync(new BrowseVenuesFilterDto
        {
            City = "Tallinn",
            MinimumCapacity = 150
        });

        var venue = Assert.Single(result.Venues);
        Assert.Equal(fixture.PrimaryVenue.Id, venue.VenueId);
        Assert.Equal("Tallinn", result.Filters.City);
        Assert.Equal(150, result.Filters.MinimumCapacity);
        Assert.Collection(
            result.AvailableCities,
            city => Assert.Equal("Riga", city),
            city => Assert.Equal("Tallinn", city));
    }

    [Fact]
    public async Task GetDashboardAsync_ScopesBookingsToVenue()
    {
        await using var context = CreateContext();
        var fixture = await SeedVenueFixtureAsync(context);
        var service = new EmployeeWorkspaceService(context);

        var dashboard = await service.GetDashboardAsync(fixture.Employee.Id, fixture.PrimaryVenue.Id);

        Assert.Equal(fixture.PrimaryVenue.Name, dashboard.VenueName);
        Assert.Single(dashboard.UpcomingBookings);
        Assert.All(dashboard.UpcomingBookings, booking => Assert.Equal("Northstar Summit", booking.Title));
    }

    [Fact]
    public async Task GetDashboardAsync_ExcludesPendingApprovalBookingsFromUpcomingCalendar()
    {
        await using var context = CreateContext();
        var fixture = await SeedVenueFixtureAsync(context);
        var service = new EmployeeWorkspaceService(context);

        context.Bookings.Add(new Booking
        {
            VenueId = fixture.PrimaryVenue.Id,
            SpaceId = fixture.Space.Id,
            CreatedByUserId = fixture.Requester.Id,
            Title = "Pending Approval Event",
            ClientName = "Requester Co",
            Status = BookingStatus.PendingApproval,
            Schedule = new ScheduleWindow(DateTime.UtcNow.AddDays(4), DateTime.UtcNow.AddDays(4).AddHours(3)),
            ExpectedAttendees = 45,
            SpaceCharge = new Money(660m, "EUR")
        });

        await context.SaveChangesAsync();

        var dashboard = await service.GetDashboardAsync(fixture.Employee.Id, fixture.PrimaryVenue.Id);

        Assert.Single(dashboard.UpcomingBookings);
        Assert.DoesNotContain(dashboard.UpcomingBookings, booking => booking.Title == "Pending Approval Event");
        Assert.Contains(dashboard.PendingApprovalBookings, booking => booking.Title == "Pending Approval Event");
    }

    [Fact]
    public async Task UpdateCateringOrderAsync_RejectsLockedOrders()
    {
        await using var context = CreateContext();
        var fixture = await SeedVenueFixtureAsync(context, lockCateringOrder: true);
        var service = new EmployeeWorkspaceService(context);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateCateringOrderAsync(
                fixture.Employee.Id,
                fixture.PrimaryVenue.Id,
                fixture.CateringOrder.Id,
                new CateringOrderEditDto
                {
                    GuestCount = 30,
                    Lines =
                    [
                        new CateringOrderLineEditDto
                        {
                            LineId = fixture.CateringLine.Id,
                            Name = fixture.CateringLine.Name,
                            Quantity = 30,
                            UnitPriceAmount = 12.5m
                        }
                    ]
                }));
    }

    [Fact]
    public async Task ApproveBookingRequestAsync_ConfirmsPendingBooking()
    {
        await using var context = CreateContext();
        var fixture = await SeedVenueFixtureAsync(context);
        var discoveryService = new PublicVenueDiscoveryService(context);
        var employeeService = new EmployeeWorkspaceService(context);

        var startsAt = DateTime.UtcNow.AddDays(12).Date.AddHours(10);
        var endsAt = startsAt.AddHours(4);

        var request = await discoveryService.SubmitBookingRequestAsync(
            fixture.Requester.Id,
            fixture.PrimaryVenue.Slug,
            new SubmitPublicBookingRequestDto
            {
                SpaceId = fixture.Space.Id,
                LayoutId = fixture.Layout.Id,
                EventTitle = "Approval Check",
                ClientName = "Signal Works",
                StartsAt = startsAt,
                EndsAt = endsAt,
                ExpectedAttendees = 60
            });

        var result = await employeeService.ApproveBookingRequestAsync(
            fixture.Manager.Id,
            fixture.PrimaryVenue.Id,
            request.BookingId);

        var booking = await context.Bookings.SingleAsync(item => item.Id == request.BookingId);
        Assert.Equal(BookingStatus.Confirmed, booking.Status);
        Assert.Equal(BookingStatus.Confirmed.ToString(), result.Status);
    }

    [Fact]
    public async Task ApproveBookingRequestAsync_ConfirmsPendingBooking_WhenDefaultTrackingIsNoTracking()
    {
        await using var context = CreateContext(useNoTrackingQueryBehavior: true);
        var fixture = await SeedVenueFixtureAsync(context);
        var discoveryService = new PublicVenueDiscoveryService(context);
        var employeeService = new EmployeeWorkspaceService(context);

        var startsAt = DateTime.UtcNow.AddDays(12).Date.AddHours(10);
        var endsAt = startsAt.AddHours(4);

        var request = await discoveryService.SubmitBookingRequestAsync(
            fixture.Requester.Id,
            fixture.PrimaryVenue.Slug,
            new SubmitPublicBookingRequestDto
            {
                SpaceId = fixture.Space.Id,
                LayoutId = fixture.Layout.Id,
                EventTitle = "Approval Check With No Tracking",
                ClientName = "Signal Works",
                StartsAt = startsAt,
                EndsAt = endsAt,
                ExpectedAttendees = 60
            });

        var result = await employeeService.ApproveBookingRequestAsync(
            fixture.Manager.Id,
            fixture.PrimaryVenue.Id,
            request.BookingId);

        var booking = await context.Bookings.SingleAsync(item => item.Id == request.BookingId);
        Assert.Equal(BookingStatus.Confirmed, booking.Status);
        Assert.Equal(BookingStatus.Confirmed.ToString(), result.Status);
    }

    [Fact]
    public async Task GetUserBookingRequestsAsync_ReturnsPendingAndApprovedBookingsForRequester()
    {
        await using var context = CreateContext();
        var fixture = await SeedVenueFixtureAsync(context);
        var discoveryService = new PublicVenueDiscoveryService(context);
        var employeeService = new EmployeeWorkspaceService(context);

        var startsAt = DateTime.UtcNow.AddDays(7).Date.AddHours(10);
        var pendingRequest = await discoveryService.SubmitBookingRequestAsync(
            fixture.Requester.Id,
            fixture.PrimaryVenue.Slug,
            new SubmitPublicBookingRequestDto
            {
                SpaceId = fixture.Space.Id,
                LayoutId = fixture.Layout.Id,
                EventTitle = "Pending Request",
                ClientName = "Requester Co",
                StartsAt = startsAt,
                EndsAt = startsAt.AddHours(2),
                ExpectedAttendees = 25
            });

        var approvedRequest = await discoveryService.SubmitBookingRequestAsync(
            fixture.Requester.Id,
            fixture.PrimaryVenue.Slug,
            new SubmitPublicBookingRequestDto
            {
                SpaceId = fixture.Space.Id,
                LayoutId = fixture.Layout.Id,
                EventTitle = "Approved Request",
                ClientName = "Requester Co",
                StartsAt = startsAt.AddDays(1),
                EndsAt = startsAt.AddDays(1).AddHours(3),
                ExpectedAttendees = 35
            });

        await employeeService.ApproveBookingRequestAsync(fixture.Manager.Id, fixture.PrimaryVenue.Id, approvedRequest.BookingId);

        var bookings = await discoveryService.GetUserBookingRequestsAsync(fixture.Requester.Id);

        var pending = Assert.Single(bookings, item => item.BookingId == pendingRequest.BookingId);
        var approved = Assert.Single(bookings, item => item.BookingId == approvedRequest.BookingId);

        Assert.Equal(BookingStatus.PendingApproval.ToString(), pending.Status);
        Assert.False(pending.AppearsOnCalendar);
        Assert.Equal(BookingStatus.Confirmed.ToString(), approved.Status);
        Assert.True(approved.AppearsOnCalendar);
    }

    [Fact]
    public async Task GetUserBookingRequestAsync_ReturnsBookingDetailsWithApprovalAndDueData()
    {
        await using var context = CreateContext();
        var fixture = await SeedVenueFixtureAsync(context);
        var discoveryService = new PublicVenueDiscoveryService(context);
        var employeeService = new EmployeeWorkspaceService(context);

        var startsAt = DateTime.UtcNow.AddDays(9).Date.AddHours(8);
        var request = await discoveryService.SubmitBookingRequestAsync(
            fixture.Requester.Id,
            fixture.PrimaryVenue.Slug,
            new SubmitPublicBookingRequestDto
            {
                SpaceId = fixture.Space.Id,
                LayoutId = fixture.Layout.Id,
                EventTitle = "Detail View Request",
                ClientName = "Requester Co",
                StartsAt = startsAt,
                EndsAt = startsAt.AddHours(5),
                ExpectedAttendees = 40,
                CateringNotes = "Tea, coffee, and pastries."
            });

        await employeeService.ApproveBookingRequestAsync(fixture.Manager.Id, fixture.PrimaryVenue.Id, request.BookingId);

        var booking = await discoveryService.GetUserBookingRequestAsync(fixture.Requester.Id, request.BookingId);

        Assert.NotNull(booking);
        Assert.Equal(BookingStatus.Confirmed.ToString(), booking!.Status);
        Assert.True(booking.AppearsOnCalendar);
        Assert.Equal(startsAt, booking.Schedule.StartsAt);
        Assert.Equal(startsAt.AddHours(-72), booking.CateringLockedAt);
        Assert.Equal("Aurora Hall", booking.SpaceName);
    }

    [Fact]
    public async Task SetActiveVenueAsync_RejectsBlockedMembership()
    {
        await using var context = CreateContext();
        var fixture = await SeedVenueFixtureAsync(context);
        var membershipService = new VenueMembershipService(context);

        await membershipService.UpdateMembershipStatusAsync(
            fixture.SecondaryMembership.Id,
            new UpdateVenueMembershipStatusDto { Status = VenueMembershipStatus.Suspended.ToString() });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            membershipService.SetActiveVenueAsync(fixture.Manager.Id, fixture.SecondaryVenue.Id));
    }

    [Fact]
    public async Task UpdateMembershipStatusAsync_ClearsActiveVenueWhenMembershipIsBlocked()
    {
        await using var context = CreateContext();
        var fixture = await SeedVenueFixtureAsync(context);
        var membershipService = new VenueMembershipService(context);

        await membershipService.UpdateMembershipStatusAsync(
            fixture.ManagerMembership.Id,
            new UpdateVenueMembershipStatusDto { Status = VenueMembershipStatus.Suspended.ToString() });

        var manager = await context.Users.SingleAsync(user => user.Id == fixture.Manager.Id);
        Assert.Null(manager.ActiveVenueId);
    }

    [Fact]
    public async Task ReviewVenueAccessRequestAsync_AssignsMembershipForApprovedRequest()
    {
        await using var context = CreateContext();
        var fixture = await SeedVenueFixtureAsync(context);
        var membershipService = new VenueMembershipService(context);
        var adminService = new VenueAdminService(context, membershipService);

        var detail = await adminService.ReviewVenueAccessRequestAsync(
            fixture.Admin.Id,
            new ReviewVenueAccessRequestDto
            {
                RequestId = fixture.PendingRequest.Id,
                Status = VenueAccessRequestStatus.Approved.ToString(),
                ReviewNotes = "Approved for pilot onboarding.",
                ApprovedAccessLevel = VenueAccessLevel.Manager.ToString(),
                AssignMembership = true
            });

        Assert.Equal(VenueAccessRequestStatus.Approved.ToString(), detail.Status);
        Assert.NotNull(detail.AssignedMembership);
        Assert.Equal(VenueAccessLevel.Manager.ToString(), detail.AssignedMembership!.AccessLevel);
    }

    [Fact]
    public async Task ReviewVenueAccessRequestAsync_PersistsChangesWithNoTrackingContext()
    {
        await using var context = CreateContext(useNoTrackingQueryBehavior: true);
        var fixture = await SeedVenueFixtureAsync(context);
        var membershipService = new VenueMembershipService(context);
        var adminService = new VenueAdminService(context, membershipService);

        await adminService.ReviewVenueAccessRequestAsync(
            fixture.Admin.Id,
            new ReviewVenueAccessRequestDto
            {
                RequestId = fixture.PendingRequest.Id,
                Status = VenueAccessRequestStatus.Approved.ToString(),
                ReviewNotes = "Approved with no-tracking context."
            });

        var savedRequest = await context.VenueAccessRequests
            .AsNoTracking()
            .SingleAsync(request => request.Id == fixture.PendingRequest.Id);

        Assert.Equal(VenueAccessRequestStatus.Approved, savedRequest.Status);
        Assert.NotNull(savedRequest.ReviewedAt);
        Assert.NotNull(savedRequest.CompanyId);
        Assert.NotNull(savedRequest.VenueId);
    }

    [Fact]
    public async Task ArchiveRejectedVenueAsync_ArchivesRejectedVenueAndPreservesRequest()
    {
        await using var context = CreateContext();
        var fixture = await SeedVenueFixtureAsync(context);
        var adminService = new VenueAdminService(context, new VenueMembershipService(context));

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
            Slug = "rejected-venue",
            City = "Tallinn",
            Country = "Estonia",
            AddressLine1 = "Sadama 7",
            Status = VenueLifecycleStatus.Active
        };

        var rejectedRequest = new VenueAccessRequest
        {
            RequestorUser = fixture.Requester,
            Company = company,
            Venue = venue,
            CompanyName = company.Name,
            VenueName = venue.Name,
            ContactName = "Marta Saar",
            ContactEmail = "marta@rejectedvenue.test",
            City = venue.City,
            Country = venue.Country,
            AddressLine1 = venue.AddressLine1,
            EstimatedMonthlyBookings = 3,
            Status = VenueAccessRequestStatus.Rejected
        };

        context.AddRange(company, venue, rejectedRequest);
        await context.SaveChangesAsync();

        foreach (var entry in context.ChangeTracker.Entries().ToList())
        {
            entry.State = EntityState.Detached;
        }

        await adminService.ArchiveRejectedVenueAsync(fixture.Admin.Id, rejectedRequest.Id);

        var archivedRequest = await context.VenueAccessRequests.SingleAsync(item => item.Id == rejectedRequest.Id);
        var archivedVenue = await context.Venues.SingleAsync(item => item.Id == venue.Id);

        Assert.Equal(VenueAccessRequestStatus.Rejected, archivedRequest.Status);
        Assert.Equal(VenueLifecycleStatus.Archived, archivedVenue.Status);
        Assert.True(await context.Companies.AnyAsync(item => item.Id == company.Id));
    }

    [Fact]
    public async Task ArchiveRejectedVenueAsync_PreservesVenueMembershipsAndActiveVenue()
    {
        await using var context = CreateContext();
        var fixture = await SeedVenueFixtureAsync(context);
        var adminService = new VenueAdminService(context, new VenueMembershipService(context));

        var company = new Company
        {
            Name = "Rejected Venue Group",
            RegistrationCode = "RVG-002",
            ContactEmail = "ops2@rejectedvenue.test"
        };

        var venue = new Venue
        {
            Company = company,
            Name = "Rejected Venue Two",
            Slug = "rejected-venue-two",
            City = "Tallinn",
            Country = "Estonia",
            AddressLine1 = "Sadama 8",
            Status = VenueLifecycleStatus.Active
        };

        var rejectedRequest = new VenueAccessRequest
        {
            RequestorUser = fixture.Requester,
            Company = company,
            Venue = venue,
            CompanyName = company.Name,
            VenueName = venue.Name,
            ContactName = "Marta Saar",
            ContactEmail = "marta@rejectedvenue.test",
            City = venue.City,
            Country = venue.Country,
            AddressLine1 = venue.AddressLine1,
            EstimatedMonthlyBookings = 3,
            Status = VenueAccessRequestStatus.Rejected
        };

        var membership = new VenueMembership
        {
            Company = company,
            Venue = venue,
            User = fixture.Requester,
            AccessLevel = VenueAccessLevel.Manager,
            Status = VenueMembershipStatus.Active,
            IsDefaultVenue = true
        };

        fixture.Requester.ActiveVenueId = venue.Id;

        context.AddRange(company, venue, rejectedRequest, membership);
        await context.SaveChangesAsync();

        foreach (var entry in context.ChangeTracker.Entries().ToList())
        {
            entry.State = EntityState.Detached;
        }

        await adminService.ArchiveRejectedVenueAsync(fixture.Admin.Id, rejectedRequest.Id);

        var archivedVenue = await context.Venues.SingleAsync(item => item.Id == venue.Id);
        Assert.Equal(VenueLifecycleStatus.Archived, archivedVenue.Status);
        Assert.True(await context.VenueMemberships.AnyAsync(item => item.VenueId == venue.Id));
        var requester = await context.Users.SingleAsync(item => item.Id == fixture.Requester.Id);
        Assert.Equal(venue.Id, requester.ActiveVenueId);
        Assert.True(await context.VenueAccessRequests.AnyAsync(item => item.Id == rejectedRequest.Id));
    }

    [Fact]
    public async Task SaveSpaceConfigurationAsync_CreatesNewSpaceForVenue()
    {
        await using var context = CreateContext();
        var fixture = await SeedVenueFixtureAsync(context);
        var adminService = new VenueAdminService(context, new VenueMembershipService(context));

        var created = await adminService.SaveSpaceConfigurationAsync(
            fixture.Manager.Id,
            fixture.PrimaryVenue.Id,
            new UpsertSpaceConfigurationDto
            {
                Name = "Skyline Studio",
                Code = "SKY",
                Status = SpaceStatus.Draft.ToString(),
                Description = "Flexible studio for workshops and hybrid sessions.",
                MinimumBookingDurationMinutes = 120,
                HourlyRateAmount = 150m,
                Currency = "EUR",
                MinimumCapacity = 8,
                RecommendedCapacity = 24,
                MaximumCapacity = 40,
                Layouts =
                [
                    new UpsertSpaceLayoutDto
                    {
                        Name = "Workshop",
                        LayoutType = LayoutType.Classroom.ToString(),
                        Capacity = 24,
                        IsDefault = true
                    }
                ]
            });

        Assert.NotEqual(Guid.Empty, created.SpaceId);
        Assert.Equal("Skyline Studio", created.Name);
        Assert.Single(created.Layouts);
        Assert.Equal(2, await context.Spaces.CountAsync(item => item.VenueId == fixture.PrimaryVenue.Id));
    }

    [Fact]
    public async Task SaveSpaceConfigurationAsync_UpdatesLayouts()
    {
        await using var context = CreateContext();
        var fixture = await SeedVenueFixtureAsync(context);
        var adminService = new VenueAdminService(context, new VenueMembershipService(context));

        var updated = await adminService.SaveSpaceConfigurationAsync(
            fixture.Manager.Id,
            fixture.PrimaryVenue.Id,
            new UpsertSpaceConfigurationDto
            {
                SpaceId = fixture.Space.Id,
                Name = "Aurora Hall",
                Code = "AUR",
                Status = SpaceStatus.Active.ToString(),
                Description = "Updated conference hall",
                MinimumBookingDurationMinutes = 90,
                HourlyRateAmount = 240m,
                Currency = "EUR",
                MinimumCapacity = 20,
                RecommendedCapacity = 100,
                MaximumCapacity = 180,
                Layouts =
                [
                    new UpsertSpaceLayoutDto
                    {
                        LayoutId = fixture.Layout.Id,
                        Name = "Theater 180",
                        LayoutType = LayoutType.Theater.ToString(),
                        Capacity = 180,
                        IsDefault = true
                    },
                    new UpsertSpaceLayoutDto
                    {
                        Name = "Banquet 120",
                        LayoutType = LayoutType.Banquet.ToString(),
                        Capacity = 120,
                        IsDefault = false
                    }
                ]
            });

        Assert.Equal(90, updated.MinimumBookingDurationMinutes);
        Assert.Equal(2, updated.Layouts.Count);
        Assert.Contains(updated.Layouts, layout => layout.LayoutType == LayoutType.Banquet.ToString());
    }

    private static AppDbContext CreateContext(bool useNoTrackingQueryBehavior = false)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .UseQueryTrackingBehavior(
                useNoTrackingQueryBehavior
                    ? QueryTrackingBehavior.NoTrackingWithIdentityResolution
                    : QueryTrackingBehavior.TrackAll)
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
        return context;
    }

    private static async Task<TestFixture> SeedVenueFixtureAsync(AppDbContext context, bool lockCateringOrder = false)
    {
        var adminRole = new AppRole { Name = AppRoles.Admin };
        context.Roles.Add(adminRole);

        var admin = new AppUser { Id = Guid.NewGuid(), Email = "admin@test.local", UserName = "admin@test.local" };
        var manager = new AppUser { Id = Guid.NewGuid(), Email = "manager@test.local", UserName = "manager@test.local" };
        var employee = new AppUser { Id = Guid.NewGuid(), Email = "employee@test.local", UserName = "employee@test.local" };
        var requester = new AppUser { Id = Guid.NewGuid(), Email = "requester@test.local", UserName = "requester@test.local" };

        context.Users.AddRange(admin, manager, employee, requester);
        context.UserRoles.Add(new IdentityUserRole<Guid> { UserId = admin.Id, RoleId = adminRole.Id });

        var company = new Company
        {
            Name = "Northstar Venue Group",
            RegistrationCode = "NSVG-001",
            ContactEmail = "ops@northstarvenues.test"
        };

        var primaryVenue = new Venue
        {
            Company = company,
            Name = "Northstar Conference Center",
            Slug = "northstar-conference-center",
            City = "Tallinn",
            Country = "Estonia",
            AddressLine1 = "Kai 14",
            Status = VenueLifecycleStatus.Active,
            DefaultHourlyRate = new Money(180m, "EUR"),
            CapacityProfile = new CapacityProfile(20, 120, 180)
        };

        var secondaryVenue = new Venue
        {
            Company = company,
            Name = "Harbor Hall",
            Slug = "harbor-hall",
            City = "Tallinn",
            Country = "Estonia",
            AddressLine1 = "Vesilennuki 6",
            Status = VenueLifecycleStatus.Active,
            DefaultHourlyRate = new Money(150m, "EUR"),
            CapacityProfile = new CapacityProfile(10, 80, 120)
        };

        var space = new Space
        {
            Venue = primaryVenue,
            Name = "Aurora Hall",
            Code = "AUR",
            Status = SpaceStatus.Active,
            MinimumBookingDurationMinutes = 60,
            HourlyRate = new Money(220m, "EUR"),
            CapacityProfile = new CapacityProfile(20, 100, 180)
        };

        var layout = new SpaceLayout
        {
            Space = space,
            Name = "Theater 180",
            LayoutType = LayoutType.Theater,
            Capacity = 180,
            IsDefault = true
        };

        var secondarySpace = new Space
        {
            Venue = secondaryVenue,
            Name = "Pier Room",
            Code = "PRM",
            Status = SpaceStatus.Active,
            MinimumBookingDurationMinutes = 60,
            HourlyRate = new Money(120m, "EUR"),
            CapacityProfile = new CapacityProfile(10, 40, 60)
        };

        var equipment = new EquipmentInventoryItem
        {
            Venue = primaryVenue,
            Name = "Projector Kit",
            Category = "AV",
            TotalQuantity = 4,
            Status = EquipmentInventoryStatus.Available,
            UnitPrice = new Money(60m, "EUR")
        };

        var managerMembership = new VenueMembership
        {
            User = manager,
            Company = company,
            Venue = primaryVenue,
            AccessLevel = VenueAccessLevel.Manager,
            Status = VenueMembershipStatus.Active,
            IsDefaultVenue = true
        };

        var secondaryMembership = new VenueMembership
        {
            User = manager,
            Company = company,
            Venue = secondaryVenue,
            AccessLevel = VenueAccessLevel.Manager,
            Status = VenueMembershipStatus.Active
        };

        var employeeMembership = new VenueMembership
        {
            User = employee,
            Company = company,
            Venue = primaryVenue,
            AccessLevel = VenueAccessLevel.Employee,
            Status = VenueMembershipStatus.Active,
            IsDefaultVenue = true
        };

        manager.ActiveVenue = primaryVenue;
        employee.ActiveVenue = primaryVenue;

        var booking = new Booking
        {
            Venue = primaryVenue,
            Space = space,
            CreatedByUser = manager,
            Title = "Northstar Summit",
            ClientName = "Helios Consulting",
            Status = BookingStatus.Confirmed,
            Schedule = new ScheduleWindow(DateTime.UtcNow.AddDays(2), DateTime.UtcNow.AddDays(2).AddHours(8)),
            ExpectedAttendees = 140,
            SpaceCharge = new Money(1200m, "EUR"),
            CoordinationNotes = "Stage setup at 07:00."
        };

        var secondaryBooking = new Booking
        {
            Venue = secondaryVenue,
            Space = secondarySpace,
            CreatedByUser = manager,
            Title = "Harbor Workshop",
            ClientName = "Aperture Studio",
            Status = BookingStatus.Confirmed,
            Schedule = new ScheduleWindow(DateTime.UtcNow.AddDays(3), DateTime.UtcNow.AddDays(3).AddHours(4)),
            ExpectedAttendees = 30,
            SpaceCharge = new Money(400m, "EUR")
        };

        var cateringOrder = new CateringOrder
        {
            Booking = booking,
            ProviderName = "Nordic Table",
            Status = CateringOrderStatus.Confirmed,
            LockedAt = lockCateringOrder ? DateTime.UtcNow.AddHours(-1) : DateTime.UtcNow.AddHours(12),
            GuestCount = 140,
            TotalPrice = new Money(1750m, "EUR")
        };

        var cateringLine = new CateringOrderLine
        {
            CateringOrder = cateringOrder,
            Name = "Lunch Buffet",
            Quantity = 140,
            UnitPrice = new Money(12.5m, "EUR")
        };

        var equipmentAllocation = new EquipmentAllocation
        {
            Booking = booking,
            EquipmentInventoryItem = equipment,
            Space = space,
            Quantity = 2,
            Status = EquipmentAllocationStatus.Confirmed,
            Schedule = new ScheduleWindow(DateTime.UtcNow.AddDays(2), DateTime.UtcNow.AddDays(2).AddHours(8)),
            TotalPrice = new Money(120m, "EUR")
        };

        var pendingRequest = new VenueAccessRequest
        {
            RequestorUser = requester,
            Company = company,
            Venue = secondaryVenue,
            CompanyName = company.Name,
            VenueName = secondaryVenue.Name,
            ContactName = "Marta Saar",
            ContactEmail = "marta@lighthouseevents.test",
            City = secondaryVenue.City,
            Country = secondaryVenue.Country,
            AddressLine1 = secondaryVenue.AddressLine1,
            EstimatedMonthlyBookings = 10,
            Status = VenueAccessRequestStatus.PendingReview
        };

        context.AddRange(
            company,
            primaryVenue,
            secondaryVenue,
            space,
            secondarySpace,
            layout,
            equipment,
            managerMembership,
            secondaryMembership,
            employeeMembership,
            booking,
            secondaryBooking,
            cateringOrder,
            cateringLine,
            equipmentAllocation,
            pendingRequest);

        await context.SaveChangesAsync();

        return new TestFixture(
            admin,
            manager,
            employee,
            requester,
            primaryVenue,
            secondaryVenue,
            managerMembership,
            secondaryMembership,
            space,
            layout,
            booking,
            cateringOrder,
            cateringLine,
            pendingRequest);
    }

    private sealed record TestFixture(
        AppUser Admin,
        AppUser Manager,
        AppUser Employee,
        AppUser Requester,
        Venue PrimaryVenue,
        Venue SecondaryVenue,
        VenueMembership ManagerMembership,
        VenueMembership SecondaryMembership,
        Space Space,
        SpaceLayout Layout,
        Booking Booking,
        CateringOrder CateringOrder,
        CateringOrderLine CateringLine,
        VenueAccessRequest PendingRequest);
}

using App.Domain.Identity;
using App.Domain.ValueObjects;
using App.Domain.Venues;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.EF.Seeding;

public static class AppDataInit
{
    public static void SeedAppData(AppDbContext context)
    {
        if (context.Companies.Any())
        {
            return;
        }

        var adminUser = context.Users.AsTracking().Single(user => user.Email == "akaver@akaver.com");
        var northstarManager = context.Users.AsTracking().Single(user => user.Email == "manager@northstarvenues.test");
        var northstarEmployee = context.Users.AsTracking().Single(user => user.Email == "employee@northstarvenues.test");
        var newVenueRequester = context.Users.AsTracking().Single(user => user.Email == "requester@newvenue.test");

        var northstarCompany = new Company
        {
            Name = "Northstar Venue Group",
            RegistrationCode = "NSVG-001",
            ContactEmail = "ops@northstarvenues.test",
            ContactPhone = "+3725550101",
            Notes = "Primary demo company for dashboard and admin flows."
        };

        var summitCompany = new Company
        {
            Name = "Summit Collective",
            RegistrationCode = "SUMMIT-002",
            ContactEmail = "hello@summitcollective.test",
            ContactPhone = "+3725550102"
        };

        var northstarVenue = new Venue
        {
            Company = northstarCompany,
            Name = "Northstar Conference Center",
            Slug = "northstar-conference-center",
            City = "Tallinn",
            Country = "Estonia",
            AddressLine1 = "Kai 14",
            Description = "Flagship venue with flexible conference spaces.",
            Status = VenueLifecycleStatus.Active,
            DefaultHourlyRate = new Money(180m, "EUR"),
            CapacityProfile = new CapacityProfile(20, 180, 240)
        };

        var harborVenue = new Venue
        {
            Company = northstarCompany,
            Name = "Harbor Hall",
            Slug = "harbor-hall",
            City = "Tallinn",
            Country = "Estonia",
            AddressLine1 = "Vesilennuki 6",
            Description = "Secondary venue used to exercise active-venue switching.",
            Status = VenueLifecycleStatus.Active,
            DefaultHourlyRate = new Money(145m, "EUR"),
            CapacityProfile = new CapacityProfile(15, 90, 120)
        };

        var summitVenue = new Venue
        {
            Company = summitCompany,
            Name = "Summit Riverside Hub",
            Slug = "summit-riverside-hub",
            City = "Tartu",
            Country = "Estonia",
            AddressLine1 = "Jõe 2",
            Description = "Public discovery example venue.",
            Status = VenueLifecycleStatus.Active,
            DefaultHourlyRate = new Money(160m, "EUR"),
            CapacityProfile = new CapacityProfile(10, 110, 160)
        };

        var auroraHall = new Space
        {
            Venue = northstarVenue,
            Name = "Aurora Hall",
            Code = "AUR",
            Description = "Main plenary hall",
            Status = SpaceStatus.Active,
            MinimumBookingDurationMinutes = 120,
            HourlyRate = new Money(220m, "EUR"),
            CapacityProfile = new CapacityProfile(40, 180, 220)
        };

        var breakoutStudio = new Space
        {
            Venue = northstarVenue,
            Name = "Breakout Studio",
            Code = "BRK",
            Description = "Workshop room for smaller sessions",
            Status = SpaceStatus.Active,
            MinimumBookingDurationMinutes = 60,
            HourlyRate = new Money(95m, "EUR"),
            CapacityProfile = new CapacityProfile(8, 28, 40)
        };

        var pierRoom = new Space
        {
            Venue = harborVenue,
            Name = "Pier Room",
            Code = "PIER",
            Description = "Waterfront meeting space",
            Status = SpaceStatus.Active,
            MinimumBookingDurationMinutes = 60,
            HourlyRate = new Money(110m, "EUR"),
            CapacityProfile = new CapacityProfile(10, 36, 48)
        };

        auroraHall.Layouts.Add(new SpaceLayout
        {
            Name = "Theater 180",
            LayoutType = LayoutType.Theater,
            Capacity = 180,
            IsDefault = true
        });
        auroraHall.Layouts.Add(new SpaceLayout
        {
            Name = "Banquet 120",
            LayoutType = LayoutType.Banquet,
            Capacity = 120
        });
        breakoutStudio.Layouts.Add(new SpaceLayout
        {
            Name = "Boardroom 20",
            LayoutType = LayoutType.Boardroom,
            Capacity = 20,
            IsDefault = true
        });
        pierRoom.Layouts.Add(new SpaceLayout
        {
            Name = "Classroom 30",
            LayoutType = LayoutType.Classroom,
            Capacity = 30,
            IsDefault = true
        });

        var projectorKit = new EquipmentInventoryItem
        {
            Venue = northstarVenue,
            Name = "4K Projector Kit",
            Category = "AV",
            TotalQuantity = 4,
            Status = EquipmentInventoryStatus.Available,
            UnitPrice = new Money(60m, "EUR")
        };

        var microphoneSet = new EquipmentInventoryItem
        {
            Venue = northstarVenue,
            Name = "Wireless Microphone Set",
            Category = "Audio",
            TotalQuantity = 8,
            Status = EquipmentInventoryStatus.Available,
            UnitPrice = new Money(18m, "EUR")
        };

        var managerMembership = new VenueMembership
        {
            Company = northstarCompany,
            Venue = northstarVenue,
            User = northstarManager,
            AccessLevel = VenueAccessLevel.Manager,
            Status = VenueMembershipStatus.Active,
            IsDefaultVenue = true,
            Notes = "Primary manager access"
        };

        var managerSecondaryMembership = new VenueMembership
        {
            Company = northstarCompany,
            Venue = harborVenue,
            User = northstarManager,
            AccessLevel = VenueAccessLevel.Manager,
            Status = VenueMembershipStatus.Active
        };

        var employeeMembership = new VenueMembership
        {
            Company = northstarCompany,
            Venue = northstarVenue,
            User = northstarEmployee,
            AccessLevel = VenueAccessLevel.Employee,
            Status = VenueMembershipStatus.Active,
            IsDefaultVenue = true
        };

        var upcomingBooking = new Booking
        {
            Venue = northstarVenue,
            Space = auroraHall,
            CreatedByUser = northstarManager,
            Title = "Baltic Product Leadership Summit",
            ClientName = "Helios Consulting",
            Status = BookingStatus.Confirmed,
            Schedule = new ScheduleWindow(DateTime.UtcNow.AddDays(3).Date.AddHours(8), DateTime.UtcNow.AddDays(3).Date.AddHours(17)),
            ExpectedAttendees = 160,
            SpaceCharge = new Money(1980m, "EUR"),
            CoordinationNotes = "Stage check at 07:30, signage at lobby and floor 2."
        };

        var draftBooking = new Booking
        {
            Venue = northstarVenue,
            Space = breakoutStudio,
            CreatedByUser = northstarEmployee,
            Title = "Quarterly Sales Enablement Workshop",
            ClientName = "Northwind Labs",
            Status = BookingStatus.PendingApproval,
            Schedule = new ScheduleWindow(DateTime.UtcNow.AddDays(8).Date.AddHours(9), DateTime.UtcNow.AddDays(8).Date.AddHours(13)),
            ExpectedAttendees = 24,
            SpaceCharge = new Money(380m, "EUR")
        };

        var harborBooking = new Booking
        {
            Venue = harborVenue,
            Space = pierRoom,
            CreatedByUser = northstarManager,
            Title = "Design Systems Offsite",
            ClientName = "Aperture Studio",
            Status = BookingStatus.Confirmed,
            Schedule = new ScheduleWindow(DateTime.UtcNow.AddDays(5).Date.AddHours(10), DateTime.UtcNow.AddDays(5).Date.AddHours(16)),
            ExpectedAttendees = 32,
            SpaceCharge = new Money(660m, "EUR")
        };

        var cateringOrder = new CateringOrder
        {
            Booking = upcomingBooking,
            ProviderName = "Nordic Table Catering",
            Status = CateringOrderStatus.Confirmed,
            LockedAt = upcomingBooking.Schedule.StartsAt.AddHours(-72),
            GuestCount = 160,
            TotalPrice = new Money(1240m, "EUR"),
            Notes = "Vegetarian and gluten-free options required."
        };

        cateringOrder.Lines.Add(new CateringOrderLine
        {
            Name = "Coffee Service",
            Quantity = 160,
            UnitPrice = new Money(3.5m, "EUR")
        });
        cateringOrder.Lines.Add(new CateringOrderLine
        {
            Name = "Buffet Lunch",
            Quantity = 160,
            UnitPrice = new Money(4.25m, "EUR"),
            DietaryNotes = "20 vegetarian, 8 gluten-free"
        });

        var equipmentAllocation = new EquipmentAllocation
        {
            Booking = upcomingBooking,
            EquipmentInventoryItem = projectorKit,
            Space = auroraHall,
            Quantity = 2,
            Status = EquipmentAllocationStatus.Confirmed,
            Schedule = new ScheduleWindow(upcomingBooking.Schedule.StartsAt.AddHours(-1), upcomingBooking.Schedule.EndsAt),
            TotalPrice = new Money(120m, "EUR")
        };

        var microphoneAllocation = new EquipmentAllocation
        {
            Booking = upcomingBooking,
            EquipmentInventoryItem = microphoneSet,
            Space = auroraHall,
            Quantity = 4,
            Status = EquipmentAllocationStatus.Confirmed,
            Schedule = new ScheduleWindow(upcomingBooking.Schedule.StartsAt.AddHours(-1), upcomingBooking.Schedule.EndsAt),
            TotalPrice = new Money(72m, "EUR")
        };

        var pendingRequest = new VenueAccessRequest
        {
            RequestorUser = newVenueRequester,
            CompanyName = "Lighthouse Events",
            VenueName = "Lighthouse Forum",
            ContactName = "Marta Saar",
            ContactEmail = "marta@lighthouseevents.test",
            ContactPhone = "+3725550199",
            City = "Pärnu",
            Country = "Estonia",
            AddressLine1 = "Ringi 8",
            EstimatedMonthlyBookings = 12,
            Status = VenueAccessRequestStatus.PendingReview,
            Notes = "Interested in onboarding one flagship venue first."
        };

        var approvedRequest = new VenueAccessRequest
        {
            RequestorUser = northstarManager,
            ReviewedByUser = adminUser,
            Company = northstarCompany,
            Venue = harborVenue,
            CompanyName = northstarCompany.Name,
            VenueName = harborVenue.Name,
            ContactName = "Northstar Ops",
            ContactEmail = northstarCompany.ContactEmail,
            City = harborVenue.City,
            Country = harborVenue.Country,
            AddressLine1 = harborVenue.AddressLine1,
            EstimatedMonthlyBookings = 20,
            Status = VenueAccessRequestStatus.Approved,
            ApprovedAccessLevel = VenueAccessLevel.Manager,
            SubmittedAt = DateTime.UtcNow.AddDays(-14),
            ReviewedAt = DateTime.UtcNow.AddDays(-10),
            ReviewNotes = "Approved during onboarding pilot."
        };

        northstarEmployee.ActiveVenue = northstarVenue;
        northstarManager.ActiveVenue = northstarVenue;

        context.AddRange(
            northstarCompany,
            summitCompany,
            northstarVenue,
            harborVenue,
            summitVenue,
            auroraHall,
            breakoutStudio,
            pierRoom,
            projectorKit,
            microphoneSet,
            managerMembership,
            managerSecondaryMembership,
            employeeMembership,
            upcomingBooking,
            draftBooking,
            harborBooking,
            cateringOrder,
            equipmentAllocation,
            microphoneAllocation,
            pendingRequest,
            approvedRequest);

        context.SaveChanges();
    }


    public static void MigrateDatabase(AppDbContext context)
    {
        context.Database.Migrate();
    }

    public static void DeleteDatabase(AppDbContext context)
    {
        context.Database.EnsureDeleted();
    }

    public static void SeedIdentity(UserManager<AppUser> userManager, RoleManager<AppRole> roleManager)
    {
        foreach (var (roleName, id) in InitialData.Roles)
        {
            var role = roleManager.FindByNameAsync(roleName).Result;

            if (role != null) continue;

            role = new AppRole()
            {
                Name = roleName,
            };

            var result = roleManager.CreateAsync(role).Result;
            if (!result.Succeeded)
            {
                throw new ApplicationException("Role creation failed!");
            }
        }


        foreach (var userInfo in InitialData.Users)
        {
            var user = userManager.FindByEmailAsync(userInfo.name).Result;
            if (user == null)
            {
                user = new AppUser()
                {
                    Email = userInfo.name,
                    UserName = userInfo.name,
                    EmailConfirmed = true
                };
                var result = userManager.CreateAsync(user, userInfo.password).Result;
                if (!result.Succeeded)
                {
                    throw new ApplicationException("User creation failed!");
                }
            }

            foreach (var role in userInfo.roles)
            {
                if (userManager.IsInRoleAsync(user, role).Result)
                {
                    Console.WriteLine($"User {user.UserName} already in role {role}");
                    continue;
                }

                var roleResult = userManager.AddToRoleAsync(user, role).Result;
                if (!roleResult.Succeeded)
                {
                    foreach (var error in roleResult.Errors)
                    {
                        Console.WriteLine(error.Description);
                    }
                }
                else
                {
                    Console.WriteLine($"User {user.UserName} added to role {role}");
                }
            }
        }
    }
}

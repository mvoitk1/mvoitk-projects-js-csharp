using App.BLL.Mappers;
using App.DAL.EF;
using App.Domain.Identity;
using App.Domain.ValueObjects;
using App.Domain.Venues;
using App.DTO.v1.Venues.Admin;
using App.DTO.v1.Venues.Membership;
using Microsoft.EntityFrameworkCore;

namespace App.BLL.Services;

public class VenueAdminService(AppDbContext context, IVenueMembershipService membershipService) : IVenueAdminService
{
    public async Task<VenueAdminDashboardDto> GetDashboardAsync(
        Guid userId,
        Guid venueId,
        CancellationToken cancellationToken = default)
    {
        await VenueAccessValidation.EnsureManagerAccessAsync(context, userId, venueId, cancellationToken);

        var venue = await context.Venues
            .AsNoTracking()
            .SingleAsync(item => item.Id == venueId, cancellationToken);

        var upcomingBookingsCount = await context.Bookings.CountAsync(
            booking => booking.VenueId == venueId &&
                       booking.Schedule.StartsAt >= DateTime.UtcNow,
            cancellationToken);

        var pendingApprovalCount = await context.Bookings.CountAsync(
            booking => booking.VenueId == venueId &&
                       booking.Status == BookingStatus.PendingApproval,
            cancellationToken);

        var activeSpacesCount = await context.Spaces.CountAsync(
            space => space.VenueId == venueId &&
                     space.Status == SpaceStatus.Active,
            cancellationToken);

        var activeMembershipCount = await context.VenueMemberships.CountAsync(
            membership => membership.VenueId == venueId &&
                          membership.Status == VenueMembershipStatus.Active,
            cancellationToken);

        var pendingRequestCount = await context.VenueAccessRequests.CountAsync(
            request => request.Status == VenueAccessRequestStatus.PendingReview ||
                       request.Status == VenueAccessRequestStatus.InReview,
            cancellationToken);

        var actionItems = new List<string>();
        if (pendingApprovalCount > 0)
        {
            actionItems.Add($"{pendingApprovalCount} bookings are waiting for approval.");
        }

        if (pendingRequestCount > 0)
        {
            actionItems.Add($"{pendingRequestCount} venue requests need triage.");
        }

        if (activeSpacesCount == 0)
        {
            actionItems.Add("No active spaces are configured for this venue.");
        }

        return new VenueAdminDashboardDto
        {
            VenueName = venue.Name,
            ActiveSpacesCount = activeSpacesCount,
            UpcomingBookingsCount = upcomingBookingsCount,
            PendingApprovalBookingsCount = pendingApprovalCount,
            PendingRequestCount = pendingRequestCount,
            ActiveMembershipCount = activeMembershipCount,
            ActionItems = actionItems
        };
    }

    public async Task<IReadOnlyList<VenueAccessRequestListItemDto>> GetVenueAccessRequestsAsync(
        CancellationToken cancellationToken = default)
    {
        var requests = await context.VenueAccessRequests
            .AsNoTracking()
            .OrderBy(request => request.Status)
            .ThenByDescending(request => request.SubmittedAt)
            .ToListAsync(cancellationToken);

        return requests.Select(request => request.ToAccessRequestListItemDto()).ToList();
    }

    public async Task<VenueAccessRequestDetailDto> GetVenueAccessRequestAsync(
        Guid requestId,
        CancellationToken cancellationToken = default)
    {
        var request = await context.VenueAccessRequests
            .Include(item => item.RequestorUser)
            .Include(item => item.ReviewedByUser)
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == requestId, cancellationToken);

        if (request == null)
        {
            throw new KeyNotFoundException("Venue access request was not found.");
        }

        var membership = request.VenueId.HasValue
            ? await context.VenueMemberships
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    item => item.UserId == request.RequestorUserId &&
                            item.VenueId == request.VenueId.Value,
                    cancellationToken)
            : null;

        return request.ToAccessRequestDetailDto(membership);
    }

    public async Task<VenueAccessRequestDetailDto> ReviewVenueAccessRequestAsync(
        Guid reviewedByUserId,
        ReviewVenueAccessRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        if (!await VenueAccessValidation.UserHasRoleAsync(context, reviewedByUserId, AppRoles.Admin, cancellationToken))
        {
            throw new InvalidOperationException("Only platform admins can review venue requests.");
        }

        if (!Enum.TryParse<VenueAccessRequestStatus>(dto.Status, true, out var status))
        {
            throw new ArgumentException("Unknown request status.", nameof(dto.Status));
        }

        var request = await context.VenueAccessRequests
            .AsTracking()
            .SingleOrDefaultAsync(item => item.Id == dto.RequestId, cancellationToken);

        if (request == null)
        {
            throw new KeyNotFoundException("Venue access request was not found.");
        }

        request.Status = status;
        request.ReviewedByUserId = reviewedByUserId;
        request.ReviewedAt = DateTime.UtcNow;
        request.ReviewNotes = string.IsNullOrWhiteSpace(dto.ReviewNotes) ? null : dto.ReviewNotes.Trim();
        request.ApprovedAccessLevel = status == VenueAccessRequestStatus.Approved && !string.IsNullOrWhiteSpace(dto.ApprovedAccessLevel)
            ? Enum.Parse<VenueAccessLevel>(dto.ApprovedAccessLevel, true)
            : null;

        if (request.Status == VenueAccessRequestStatus.Approved)
        {
            await EnsureApprovedRequestLinkedVenueAsync(request, cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);

        if (dto.AssignMembership)
        {
            if (request.Status != VenueAccessRequestStatus.Approved)
            {
                throw new InvalidOperationException("Memberships can only be assigned after approval.");
            }

            if (!request.VenueId.HasValue || !request.CompanyId.HasValue || request.ApprovedAccessLevel == null)
            {
                throw new InvalidOperationException("Approved venue, company, and access level are required before assigning rights.");
            }

            await membershipService.AssignVenueMembershipAsync(
                new AssignVenueMembershipDto
                {
                    UserId = request.RequestorUserId,
                    CompanyId = request.CompanyId.Value,
                    VenueId = request.VenueId.Value,
                    AccessLevel = request.ApprovedAccessLevel.Value.ToString(),
                    IsDefaultVenue = !await context.VenueMemberships.AnyAsync(
                        item => item.UserId == request.RequestorUserId && item.Status == VenueMembershipStatus.Active,
                        cancellationToken),
                    Notes = request.ReviewNotes,
                    SourceRequestId = request.Id
                },
                cancellationToken);
        }

        return await GetVenueAccessRequestAsync(request.Id, cancellationToken);
    }

    public async Task ArchiveRejectedVenueAsync(
        Guid reviewedByUserId,
        Guid requestId,
        CancellationToken cancellationToken = default)
    {
        if (!await VenueAccessValidation.UserHasRoleAsync(context, reviewedByUserId, AppRoles.Admin, cancellationToken))
        {
            throw new InvalidOperationException("Only platform admins can archive rejected venues.");
        }

        var request = await context.VenueAccessRequests
            .AsTracking()
            .SingleOrDefaultAsync(item => item.Id == requestId, cancellationToken);

        if (request == null)
        {
            throw new KeyNotFoundException("Venue access request was not found.");
        }

        if (request.Status != VenueAccessRequestStatus.Rejected)
        {
            throw new InvalidOperationException("Only rejected venue requests can archive their linked venue.");
        }

        if (!request.VenueId.HasValue)
        {
            throw new InvalidOperationException("This rejected request is not linked to a venue that can be archived.");
        }

        var venueId = request.VenueId.Value;
        var venue = await context.Venues
            .AsTracking()
            .SingleOrDefaultAsync(item => item.Id == venueId, cancellationToken);

        if (venue == null)
        {
            throw new KeyNotFoundException("Venue was not found.");
        }

        venue.Status = VenueLifecycleStatus.Archived;

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureApprovedRequestLinkedVenueAsync(
        VenueAccessRequest request,
        CancellationToken cancellationToken)
    {
        if (request.CompanyId.HasValue && request.VenueId.HasValue)
        {
            return;
        }

        var company = request.CompanyId.HasValue
            ? await context.Companies.AsTracking().SingleAsync(item => item.Id == request.CompanyId.Value, cancellationToken)
            : await context.Companies.AsTracking().SingleOrDefaultAsync(
                item => item.Name == request.CompanyName,
                cancellationToken);

        if (company == null)
        {
            company = new Company
            {
                Name = request.CompanyName.Trim(),
                RegistrationCode = await GenerateUniqueRegistrationCodeAsync(request.CompanyName, cancellationToken),
                ContactEmail = request.ContactEmail.Trim(),
                ContactPhone = string.IsNullOrWhiteSpace(request.ContactPhone) ? null : request.ContactPhone.Trim(),
                Notes = request.Notes
            };

            context.Companies.Add(company);
        }

        request.Company = company;

        var venue = request.VenueId.HasValue
            ? await context.Venues.AsTracking().SingleAsync(item => item.Id == request.VenueId.Value, cancellationToken)
            : await context.Venues.AsTracking().SingleOrDefaultAsync(
                item => item.CompanyId == company.Id &&
                        item.Name == request.VenueName,
                cancellationToken);

        if (venue == null)
        {
            venue = new Venue
            {
                Company = company,
                Name = request.VenueName.Trim(),
                Slug = await GenerateUniqueVenueSlugAsync(request.VenueName, cancellationToken),
                City = request.City.Trim(),
                Country = request.Country.Trim(),
                AddressLine1 = request.AddressLine1.Trim(),
                Description = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
                Status = VenueLifecycleStatus.Active
            };

            context.Venues.Add(venue);
        }

        request.Venue = venue;
    }

    private async Task<string> GenerateUniqueRegistrationCodeAsync(string companyName, CancellationToken cancellationToken)
    {
        var stem = new string(companyName
            .ToUpperInvariant()
            .Where(char.IsLetterOrDigit)
            .Take(12)
            .ToArray());

        if (string.IsNullOrWhiteSpace(stem))
        {
            stem = "VENUE";
        }

        for (var suffix = 1; suffix < 10_000; suffix++)
        {
            var candidate = $"{stem}-{suffix:D3}";
            var exists = await context.Companies.AnyAsync(item => item.RegistrationCode == candidate, cancellationToken);
            if (!exists)
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("Unable to generate a unique company registration code.");
    }

    private async Task<string> GenerateUniqueVenueSlugAsync(string venueName, CancellationToken cancellationToken)
    {
        var normalized = new string(venueName
            .Trim()
            .ToLowerInvariant()
            .Select(character => char.IsLetterOrDigit(character) ? character : '-')
            .ToArray());

        var parts = normalized
            .Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var stem = string.Join('-', parts);

        if (string.IsNullOrWhiteSpace(stem))
        {
            stem = "venue";
        }

        for (var suffix = 0; suffix < 10_000; suffix++)
        {
            var candidate = suffix == 0 ? stem : $"{stem}-{suffix}";
            var exists = await context.Venues.AnyAsync(item => item.Slug == candidate, cancellationToken);
            if (!exists)
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("Unable to generate a unique venue slug.");
    }

    public async Task<IReadOnlyList<SpaceConfigurationDto>> GetSpaceConfigurationsAsync(
        Guid userId,
        Guid venueId,
        CancellationToken cancellationToken = default)
    {
        await VenueAccessValidation.EnsureManagerAccessAsync(context, userId, venueId, cancellationToken);

        var spaces = await context.Spaces
            .AsNoTracking()
            .Where(space => space.VenueId == venueId)
            .Include(space => space.Layouts)
            .OrderBy(space => space.Name)
            .ToListAsync(cancellationToken);

        return spaces.Select(space => space.ToSpaceConfigurationDto()).ToList();
    }

    public async Task<SpaceConfigurationDto> SaveSpaceConfigurationAsync(
        Guid userId,
        Guid venueId,
        UpsertSpaceConfigurationDto dto,
        CancellationToken cancellationToken = default)
    {
        await VenueAccessValidation.EnsureManagerAccessAsync(context, userId, venueId, cancellationToken);

        if (!Enum.TryParse<SpaceStatus>(dto.Status, true, out var status))
        {
            throw new ArgumentException("Unknown space status.", nameof(dto.Status));
        }

        if (dto.MinimumBookingDurationMinutes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dto.MinimumBookingDurationMinutes));
        }

        var space = dto.SpaceId.HasValue
            ? await context.Spaces
                .AsTracking()
                .Include(item => item.Layouts)
                .SingleOrDefaultAsync(item => item.Id == dto.SpaceId.Value && item.VenueId == venueId, cancellationToken)
            : null;

        if (space == null)
        {
            space = new Space
            {
                VenueId = venueId
            };
            context.Spaces.Add(space);
        }

        space.Name = dto.Name.Trim();
        space.Code = dto.Code.Trim().ToUpperInvariant();
        space.Status = status;
        space.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
        space.MinimumBookingDurationMinutes = dto.MinimumBookingDurationMinutes;
        space.HourlyRate = new Money(dto.HourlyRateAmount, dto.Currency);
        space.CapacityProfile = new CapacityProfile(dto.MinimumCapacity, dto.RecommendedCapacity, dto.MaximumCapacity);

        var layoutsById = space.Layouts.ToDictionary(layout => layout.Id);
        var keptLayoutIds = new HashSet<Guid>();

        foreach (var layoutDto in dto.Layouts)
        {
            if (!Enum.TryParse<LayoutType>(layoutDto.LayoutType, true, out var layoutType))
            {
                throw new ArgumentException("Unknown layout type.", nameof(layoutDto.LayoutType));
            }

            SpaceLayout? matchedLayout = null;
            var isExistingLayout = layoutDto.LayoutId.HasValue &&
                                   layoutsById.TryGetValue(layoutDto.LayoutId.Value, out matchedLayout);
            var layout = isExistingLayout
                ? matchedLayout!
                : new SpaceLayout { Space = space };

            layout.Name = layoutDto.Name.Trim();
            layout.LayoutType = layoutType;
            layout.Capacity = layoutDto.Capacity;
            layout.IsDefault = layoutDto.IsDefault;
            layout.Notes = string.IsNullOrWhiteSpace(layoutDto.Notes) ? null : layoutDto.Notes.Trim();

            if (!isExistingLayout)
            {
                space.Layouts.Add(layout);
                context.Entry(layout).State = EntityState.Added;
            }

            keptLayoutIds.Add(layout.Id);
        }

        var layoutsToRemove = space.Layouts
            .Where(layout => layout.Id != Guid.Empty && !keptLayoutIds.Contains(layout.Id))
            .ToList();

        foreach (var layout in layoutsToRemove)
        {
            space.Layouts.Remove(layout);
            context.Remove(layout);
        }

        if (space.Layouts.Count(layout => layout.IsDefault) > 1)
        {
            throw new InvalidOperationException("Only one default layout is allowed per space.");
        }

        await context.SaveChangesAsync(cancellationToken);

        space = await context.Spaces
            .AsNoTracking()
            .Include(item => item.Layouts)
            .SingleAsync(item => item.Id == space.Id, cancellationToken);

        return space.ToSpaceConfigurationDto();
    }
}

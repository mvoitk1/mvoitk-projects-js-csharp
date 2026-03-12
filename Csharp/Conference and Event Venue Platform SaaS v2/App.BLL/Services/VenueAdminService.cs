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

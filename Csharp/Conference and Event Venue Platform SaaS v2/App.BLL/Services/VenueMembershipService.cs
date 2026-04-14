using App.BLL.Mappers;
using App.DAL.EF;
using App.Domain.Venues;
using App.DTO.v1.Venues.Membership;
using Microsoft.EntityFrameworkCore;

namespace App.BLL.Services;

public class VenueMembershipService(AppDbContext context) : IVenueMembershipService
{
    public async Task<IReadOnlyList<ActiveVenueOptionDto>> GetVenueOptionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var activeVenueId = await context.Users
            .AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => user.ActiveVenueId)
            .SingleOrDefaultAsync(cancellationToken);

        var memberships = await context.VenueMemberships
            .AsNoTracking()
            .Where(membership => membership.UserId == userId)
            .Include(membership => membership.Company)
            .Include(membership => membership.Venue)
            .OrderByDescending(membership => membership.IsDefaultVenue)
            .ThenBy(membership => membership.Venue.Name)
            .ToListAsync(cancellationToken);

        return memberships.Select(membership => membership.ToActiveVenueOptionDto(activeVenueId)).ToList();
    }

    public async Task<ActiveVenueSelectionResultDto?> GetActiveVenueAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await context.Users
            .AsNoTracking()
            .Where(item => item.Id == userId)
            .Select(item => new { item.Id, item.ActiveVenueId })
            .SingleOrDefaultAsync(cancellationToken);

        if (user?.ActiveVenueId == null)
        {
            return null;
        }

        var membership = await context.VenueMemberships
            .AsNoTracking()
            .Include(item => item.Company)
            .Include(item => item.Venue)
            .SingleOrDefaultAsync(
                item => item.UserId == userId &&
                        item.VenueId == user.ActiveVenueId.Value,
                cancellationToken);

        if (membership == null)
        {
            return null;
        }

        var result = membership.ToActiveVenueSelectionResultDto();
        var rejectedRequest = await context.VenueAccessRequests
            .AsNoTracking()
            .Where(item => item.VenueId == membership.VenueId && item.Status == VenueAccessRequestStatus.Rejected)
            .OrderByDescending(item => item.ReviewedAt ?? item.SubmittedAt)
            .Select(item => item.ReviewNotes)
            .FirstOrDefaultAsync(cancellationToken);

        return new ActiveVenueSelectionResultDto
        {
            VenueId = result.VenueId,
            MembershipId = result.MembershipId,
            VenueName = result.VenueName,
            CompanyName = result.CompanyName,
            AccessLevel = result.AccessLevel,
            VenueStatus = result.VenueStatus,
            IsRejectedVenue = membership.Venue.Status == VenueLifecycleStatus.Archived && rejectedRequest != null,
            RejectionNotes = rejectedRequest
        };
    }

    public async Task<ActiveVenueSelectionResultDto> SetActiveVenueAsync(
        Guid userId,
        Guid venueId,
        CancellationToken cancellationToken = default)
    {
        var membership = await context.VenueMemberships
            .AsTracking()
            .Include(item => item.Company)
            .Include(item => item.Venue)
            .SingleOrDefaultAsync(
                item => item.UserId == userId &&
                        item.VenueId == venueId,
                cancellationToken);

        if (membership == null)
        {
            throw new KeyNotFoundException("Venue membership was not found.");
        }

        if (membership.Status.BlocksVenueSelection())
        {
            throw new InvalidOperationException("This venue membership is blocked from active selection.");
        }

        var user = await context.Users.AsTracking().SingleAsync(item => item.Id == userId, cancellationToken);
        user.ActiveVenueId = venueId;

        var memberships = await context.VenueMemberships
            .AsTracking()
            .Where(item => item.UserId == userId)
            .ToListAsync(cancellationToken);

        foreach (var item in memberships)
        {
            item.IsDefaultVenue = item.VenueId == venueId;
        }

        await context.SaveChangesAsync(cancellationToken);
        return membership.ToActiveVenueSelectionResultDto();
    }

    public async Task<VenueMembershipSummaryDto> AssignVenueMembershipAsync(
        AssignVenueMembershipDto dto,
        CancellationToken cancellationToken = default)
    {
        var accessLevel = ParseAccessLevel(dto.AccessLevel);

        if (dto.SourceRequestId.HasValue)
        {
            var sourceRequest = await context.VenueAccessRequests.SingleOrDefaultAsync(
                item => item.Id == dto.SourceRequestId.Value,
                cancellationToken);

            if (sourceRequest == null)
            {
                throw new KeyNotFoundException("Venue access request was not found.");
            }

            if (!sourceRequest.Status.AllowsMembershipAssignment())
            {
                throw new InvalidOperationException("Venue rights can only be assigned from approved requests.");
            }
        }

        var membership = await context.VenueMemberships
            .AsTracking()
            .Include(item => item.Company)
            .Include(item => item.Venue)
            .SingleOrDefaultAsync(
                item => item.UserId == dto.UserId &&
                        item.VenueId == dto.VenueId,
                cancellationToken);

        if (membership == null)
        {
            membership = new VenueMembership
            {
                UserId = dto.UserId,
                CompanyId = dto.CompanyId,
                VenueId = dto.VenueId
            };

            context.VenueMemberships.Add(membership);
        }

        membership.AccessLevel = accessLevel;
        membership.Status = VenueMembershipStatus.Active;
        membership.IsDefaultVenue = dto.IsDefaultVenue;
        membership.GrantedAt = DateTime.UtcNow;
        membership.BlockedAt = null;
        membership.Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim();

        if (dto.IsDefaultVenue)
        {
            var user = await context.Users.AsTracking().SingleAsync(item => item.Id == dto.UserId, cancellationToken);
            user.ActiveVenueId = dto.VenueId;

            var otherMemberships = await context.VenueMemberships
                .AsTracking()
                .Where(item => item.UserId == dto.UserId && item.Id != membership.Id)
                .ToListAsync(cancellationToken);

            foreach (var item in otherMemberships)
            {
                item.IsDefaultVenue = false;
            }
        }

        await context.SaveChangesAsync(cancellationToken);

        membership = await context.VenueMemberships
            .AsNoTracking()
            .Include(item => item.Company)
            .Include(item => item.Venue)
            .SingleAsync(item => item.Id == membership.Id, cancellationToken);

        return membership.ToMembershipSummaryDto();
    }

    public async Task<VenueMembershipSummaryDto> UpdateMembershipStatusAsync(
        Guid membershipId,
        UpdateVenueMembershipStatusDto dto,
        CancellationToken cancellationToken = default)
    {
        var status = ParseMembershipStatus(dto.Status);

        var membership = await context.VenueMemberships
            .AsTracking()
            .Include(item => item.Company)
            .Include(item => item.Venue)
            .SingleOrDefaultAsync(item => item.Id == membershipId, cancellationToken);

        if (membership == null)
        {
            throw new KeyNotFoundException("Venue membership was not found.");
        }

        membership.Status = status;
        membership.Notes = string.IsNullOrWhiteSpace(dto.Notes) ? membership.Notes : dto.Notes.Trim();
        membership.BlockedAt = status.CanOperate() ? null : DateTime.UtcNow;

        if (!status.CanOperate())
        {
            membership.IsDefaultVenue = false;
            var user = await context.Users.AsTracking().SingleAsync(item => item.Id == membership.UserId, cancellationToken);
            if (user.ActiveVenueId == membership.VenueId)
            {
                user.ActiveVenueId = null;
            }
        }

        await context.SaveChangesAsync(cancellationToken);
        return membership.ToMembershipSummaryDto();
    }

    private static VenueAccessLevel ParseAccessLevel(string value)
    {
        if (!Enum.TryParse<VenueAccessLevel>(value, true, out var accessLevel))
        {
            throw new ArgumentException("Unknown venue access level.", nameof(value));
        }

        return accessLevel;
    }

    private static VenueMembershipStatus ParseMembershipStatus(string value)
    {
        if (!Enum.TryParse<VenueMembershipStatus>(value, true, out var status))
        {
            throw new ArgumentException("Unknown membership status.", nameof(value));
        }

        return status;
    }
}

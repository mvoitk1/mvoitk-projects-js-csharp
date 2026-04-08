using App.Domain.Venues;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.DAL.EF.Configurations;

public class VenueMembershipConfiguration : IEntityTypeConfiguration<VenueMembership>
{
    public void Configure(EntityTypeBuilder<VenueMembership> builder)
    {
        builder.Property(membership => membership.Notes)
            .HasMaxLength(2000);

        builder.HasIndex(membership => new { membership.UserId, membership.VenueId })
            .IsUnique();
    }
}

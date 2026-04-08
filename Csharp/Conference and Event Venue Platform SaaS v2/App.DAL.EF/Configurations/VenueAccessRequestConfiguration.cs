using App.Domain.Venues;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.DAL.EF.Configurations;

public class VenueAccessRequestConfiguration : IEntityTypeConfiguration<VenueAccessRequest>
{
    public void Configure(EntityTypeBuilder<VenueAccessRequest> builder)
    {
        builder.Property(request => request.CompanyName)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(request => request.VenueName)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(request => request.ContactName)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(request => request.ContactEmail)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(request => request.ContactPhone)
            .HasMaxLength(64);

        builder.Property(request => request.City)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(request => request.Country)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(request => request.AddressLine1)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(request => request.Notes)
            .HasMaxLength(2000);

        builder.Property(request => request.ReviewNotes)
            .HasMaxLength(2000);

        builder.HasOne(request => request.RequestorUser)
            .WithMany(user => user.SubmittedVenueAccessRequests)
            .HasForeignKey(request => request.RequestorUserId);

        builder.HasOne(request => request.ReviewedByUser)
            .WithMany(user => user.ReviewedVenueAccessRequests)
            .HasForeignKey(request => request.ReviewedByUserId);
    }
}

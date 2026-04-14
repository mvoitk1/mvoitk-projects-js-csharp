using App.Domain.ValueObjects;
using App.Domain.Venues;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.DAL.EF.Configurations;

public class SpaceConfiguration : IEntityTypeConfiguration<Space>
{
    public void Configure(EntityTypeBuilder<Space> builder)
    {
        builder.Property(space => space.Name)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(space => space.Code)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(space => space.Description)
            .HasMaxLength(2000);

        builder.HasIndex(space => new { space.VenueId, space.Code })
            .IsUnique();

        builder.HasOne(space => space.ParentSpace)
            .WithMany(space => space.CombinedSpaces)
            .HasForeignKey(space => space.ParentSpaceId);

        builder.OwnsOne(space => space.HourlyRate, money =>
        {
            money.Property(value => value.Amount)
                .HasColumnName("HourlyRateAmount")
                .HasPrecision(18, 2);

            money.Property(value => value.Currency)
                .HasColumnName("HourlyRateCurrency")
                .HasMaxLength(3);
        });

        builder.OwnsOne(space => space.CapacityProfile, capacity =>
        {
            capacity.Property(value => value.Minimum).HasColumnName("CapacityMinimum");
            capacity.Property(value => value.Recommended).HasColumnName("CapacityRecommended");
            capacity.Property(value => value.Maximum).HasColumnName("CapacityMaximum");
        });
    }
}

using App.Domain.ValueObjects;
using App.Domain.Venues;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.DAL.EF.Configurations;

public class VenueConfiguration : IEntityTypeConfiguration<Venue>
{
    public void Configure(EntityTypeBuilder<Venue> builder)
    {
        builder.Property(venue => venue.Name)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(venue => venue.Slug)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(venue => venue.City)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(venue => venue.Country)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(venue => venue.AddressLine1)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(venue => venue.AddressLine2)
            .HasMaxLength(256);

        builder.Property(venue => venue.Description)
            .HasMaxLength(4000);

        builder.HasIndex(venue => venue.Slug)
            .IsUnique();

        builder.OwnsOne(venue => venue.DefaultHourlyRate, money =>
        {
            ConfigureMoney(money, "DefaultHourlyRate");
        });

        builder.OwnsOne(venue => venue.CapacityProfile, capacity =>
        {
            ConfigureCapacity(capacity, "Capacity");
        });
    }

    private static void ConfigureMoney(OwnedNavigationBuilder<Venue, Money> builder, string prefix)
    {
        builder.Property(value => value.Amount)
            .HasColumnName($"{prefix}Amount")
            .HasPrecision(18, 2);

        builder.Property(value => value.Currency)
            .HasColumnName($"{prefix}Currency")
            .HasMaxLength(3);
    }

    private static void ConfigureCapacity(OwnedNavigationBuilder<Venue, CapacityProfile> builder, string prefix)
    {
        builder.Property(value => value.Minimum).HasColumnName($"{prefix}Minimum");
        builder.Property(value => value.Recommended).HasColumnName($"{prefix}Recommended");
        builder.Property(value => value.Maximum).HasColumnName($"{prefix}Maximum");
    }
}

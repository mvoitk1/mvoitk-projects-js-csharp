using App.Domain.ValueObjects;
using App.Domain.Venues;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.DAL.EF.Configurations;

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.Property(booking => booking.Title)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(booking => booking.ClientName)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(booking => booking.CoordinationNotes)
            .HasMaxLength(4000);

        builder.HasOne(booking => booking.CreatedByUser)
            .WithMany(user => user.CreatedBookings)
            .HasForeignKey(booking => booking.CreatedByUserId);

        builder.OwnsOne(booking => booking.Schedule, schedule =>
        {
            schedule.Property(value => value.StartsAt).HasColumnName("StartsAt");
            schedule.Property(value => value.EndsAt).HasColumnName("EndsAt");
        });

        builder.OwnsOne(booking => booking.SpaceCharge, money =>
        {
            money.Property(value => value.Amount)
                .HasColumnName("SpaceChargeAmount")
                .HasPrecision(18, 2);

            money.Property(value => value.Currency)
                .HasColumnName("SpaceChargeCurrency")
                .HasMaxLength(3);
        });
    }
}

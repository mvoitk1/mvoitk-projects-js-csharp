using App.Domain.Venues;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.DAL.EF.Configurations;

public class EquipmentAllocationConfiguration : IEntityTypeConfiguration<EquipmentAllocation>
{
    public void Configure(EntityTypeBuilder<EquipmentAllocation> builder)
    {
        builder.Property(allocation => allocation.Notes)
            .HasMaxLength(2000);

        builder.OwnsOne(allocation => allocation.Schedule, schedule =>
        {
            schedule.Property(value => value.StartsAt).HasColumnName("StartsAt");
            schedule.Property(value => value.EndsAt).HasColumnName("EndsAt");
        });

        builder.OwnsOne(allocation => allocation.TotalPrice, money =>
        {
            money.Property(value => value.Amount)
                .HasColumnName("TotalPriceAmount")
                .HasPrecision(18, 2);

            money.Property(value => value.Currency)
                .HasColumnName("TotalPriceCurrency")
                .HasMaxLength(3);
        });
    }
}

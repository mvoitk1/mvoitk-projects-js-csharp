using App.Domain.Venues;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.DAL.EF.Configurations;

public class EquipmentInventoryItemConfiguration : IEntityTypeConfiguration<EquipmentInventoryItem>
{
    public void Configure(EntityTypeBuilder<EquipmentInventoryItem> builder)
    {
        builder.Property(item => item.Name)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(item => item.Category)
            .HasMaxLength(128)
            .IsRequired();

        builder.HasIndex(item => new { item.VenueId, item.Name })
            .IsUnique();

        builder.OwnsOne(item => item.UnitPrice, money =>
        {
            money.Property(value => value.Amount)
                .HasColumnName("UnitPriceAmount")
                .HasPrecision(18, 2);

            money.Property(value => value.Currency)
                .HasColumnName("UnitPriceCurrency")
                .HasMaxLength(3);
        });
    }
}

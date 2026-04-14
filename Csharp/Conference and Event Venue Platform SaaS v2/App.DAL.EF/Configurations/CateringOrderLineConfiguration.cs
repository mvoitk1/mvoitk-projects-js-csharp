using App.Domain.Venues;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.DAL.EF.Configurations;

public class CateringOrderLineConfiguration : IEntityTypeConfiguration<CateringOrderLine>
{
    public void Configure(EntityTypeBuilder<CateringOrderLine> builder)
    {
        builder.Property(line => line.Name)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(line => line.DietaryNotes)
            .HasMaxLength(1000);

        builder.OwnsOne(line => line.UnitPrice, money =>
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

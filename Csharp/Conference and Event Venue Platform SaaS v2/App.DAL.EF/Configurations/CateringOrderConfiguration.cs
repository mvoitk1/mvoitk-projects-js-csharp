using App.Domain.Venues;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.DAL.EF.Configurations;

public class CateringOrderConfiguration : IEntityTypeConfiguration<CateringOrder>
{
    public void Configure(EntityTypeBuilder<CateringOrder> builder)
    {
        builder.Property(order => order.ProviderName)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(order => order.Notes)
            .HasMaxLength(2000);

        builder.OwnsOne(order => order.TotalPrice, money =>
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

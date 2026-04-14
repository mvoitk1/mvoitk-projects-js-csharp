using App.Domain.Venues;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.DAL.EF.Configurations;

public class SpaceLayoutConfiguration : IEntityTypeConfiguration<SpaceLayout>
{
    public void Configure(EntityTypeBuilder<SpaceLayout> builder)
    {
        builder.Property(layout => layout.Name)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(layout => layout.Notes)
            .HasMaxLength(2000);

        builder.HasIndex(layout => new { layout.SpaceId, layout.LayoutType, layout.Name })
            .IsUnique();
    }
}

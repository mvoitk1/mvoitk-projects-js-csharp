using App.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.DAL.EF.Configurations;

public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.HasOne(user => user.ActiveVenue)
            .WithMany(venue => venue.ActiveUsers)
            .HasForeignKey(user => user.ActiveVenueId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

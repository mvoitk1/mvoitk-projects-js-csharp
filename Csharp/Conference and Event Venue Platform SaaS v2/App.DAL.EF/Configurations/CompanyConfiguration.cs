using App.Domain.Venues;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.DAL.EF.Configurations;

public class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.Property(company => company.Name)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(company => company.RegistrationCode)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(company => company.ContactEmail)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(company => company.ContactPhone)
            .HasMaxLength(64);

        builder.Property(company => company.Notes)
            .HasMaxLength(2000);

        builder.HasIndex(company => company.RegistrationCode)
            .IsUnique();
    }
}

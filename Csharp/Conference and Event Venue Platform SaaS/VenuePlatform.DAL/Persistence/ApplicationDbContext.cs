using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VenuePlatform.BLL.Domain.Companies;
using VenuePlatform.BLL.Domain.Clients;
using VenuePlatform.BLL.Domain.Auth;
using VenuePlatform.BLL.Domain.Spaces;

namespace VenuePlatform.DAL.Persistence;

public sealed class ApplicationDbContext : IdentityDbContext<IdentityUser<Guid>, IdentityRole<Guid>, Guid>
{
    private readonly ITenantProvider? _tenantProvider;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ITenantProvider? tenantProvider)
        : base(options)
    {
        _tenantProvider = tenantProvider;
    }

    public Guid? CurrentCompanyId => _tenantProvider?.CurrentCompanyId;

    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<UserCompanyMembership> UserCompanyMemberships => Set<UserCompanyMembership>();
    public DbSet<Space> Spaces => Set<Space>();
    public DbSet<SpaceConfiguration> SpaceConfigurations => Set<SpaceConfiguration>();
    public DbSet<SpaceConfigurationSpace> SpaceConfigurationSpaces => Set<SpaceConfigurationSpace>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var company = modelBuilder.Entity<Company>();

        company.HasKey(c => c.Id);

        company.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);

        company.Property(c => c.Slug)
            .IsRequired()
            .HasMaxLength(64);

        company.HasIndex(c => c.Slug)
            .IsUnique();

        company.Property(c => c.CreatedUtc)
            .IsRequired();

        // Client configuration with tenant query filter
        var client = modelBuilder.Entity<Client>();

        client.ToTable("Clients");

        client.HasKey(c => c.Id);

        client.Property(c => c.CompanyId)
            .IsRequired();

        client.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);

        client.Property(c => c.Notes)
            .HasMaxLength(2000);

        client.Property(c => c.CreatedUtc)
            .IsRequired();

        client.HasIndex(c => new { c.CompanyId, c.Name });

        // Tenant query filter - applied to Client and Space
        var companyId = CurrentCompanyId;
        client.HasQueryFilter(c => companyId != null && c.CompanyId == companyId);

        // Space configuration with tenant query filter
        var space = modelBuilder.Entity<Space>();

        space.ToTable("Spaces");

        space.HasKey(s => s.Id);

        space.Property(s => s.CompanyId)
            .IsRequired();

        space.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(200);

        space.Property(s => s.Capacity)
            .IsRequired();

        space.Property(s => s.HourlyRate)
            .IsRequired()
            .HasPrecision(18, 2);

        space.Property(s => s.CreatedUtc)
            .IsRequired();

        space.HasIndex(s => new { s.CompanyId, s.Name });

        // Tenant query filter for Space
        space.HasQueryFilter(s => companyId != null && s.CompanyId == companyId);

        // SpaceConfiguration configuration with tenant query filter
        var spaceConfig = modelBuilder.Entity<SpaceConfiguration>();

        spaceConfig.ToTable("SpaceConfigurations");

        spaceConfig.HasKey(sc => sc.Id);

        spaceConfig.Property(sc => sc.CompanyId)
            .IsRequired();

        spaceConfig.Property(sc => sc.Name)
            .IsRequired()
            .HasMaxLength(200);

        spaceConfig.Property(sc => sc.HourlyRateOverride)
            .HasPrecision(18, 2);

        spaceConfig.Property(sc => sc.MinBookingMinutesOverride);

        spaceConfig.Property(sc => sc.CreatedUtc)
            .IsRequired();

        spaceConfig.HasIndex(sc => new { sc.CompanyId, sc.Name });

        // Tenant query filter for SpaceConfiguration
        spaceConfig.HasQueryFilter(sc => companyId != null && sc.CompanyId == companyId);

        // UserCompanyMembership configuration - pure join table with composite key
        var membership = modelBuilder.Entity<UserCompanyMembership>();

        membership.ToTable("UserCompanyMemberships");

        membership.HasKey(x => new { x.UserId, x.CompanyId });

        membership.Property(m => m.UserId)
            .IsRequired();

        membership.Property(m => m.CompanyId)
            .IsRequired();

        membership.Property(m => m.Role)
            .IsRequired()
            .HasMaxLength(50);

        membership.Property(m => m.CreatedUtc)
            .IsRequired();

        // Index for fast lookups by company
        membership.HasIndex(m => m.CompanyId);

        // SpaceConfigurationSpace join table configuration
        var spaceConfigSpace = modelBuilder.Entity<SpaceConfigurationSpace>();

        spaceConfigSpace.ToTable("SpaceConfigurationSpaces");

        // Composite key
        spaceConfigSpace.HasKey(x => new { x.SpaceConfigurationId, x.SpaceId });

        spaceConfigSpace.Property(x => x.SpaceConfigurationId)
            .IsRequired();

        spaceConfigSpace.Property(x => x.SpaceId)
            .IsRequired();

        // Foreign key to SpaceConfigurations with Cascade delete
        spaceConfigSpace.HasOne<SpaceConfiguration>()
            .WithMany()
            .HasForeignKey(x => x.SpaceConfigurationId)
            .OnDelete(DeleteBehavior.Cascade);

        // Foreign key to Spaces with Restrict delete
        spaceConfigSpace.HasOne<Space>()
            .WithMany()
            .HasForeignKey(x => x.SpaceId)
            .OnDelete(DeleteBehavior.Restrict);

        // Index on SpaceConfigurationId for fast lookups
        spaceConfigSpace.HasIndex(x => x.SpaceConfigurationId);
    }
}

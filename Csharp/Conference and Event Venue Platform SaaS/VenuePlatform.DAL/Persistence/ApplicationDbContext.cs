using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VenuePlatform.BLL.Domain.Companies;
using VenuePlatform.BLL.Domain.Clients;
using VenuePlatform.BLL.Domain.Auth;
using VenuePlatform.BLL.Domain.Spaces;
using VenuePlatform.BLL.Domain.Bookings;
using VenuePlatform.BLL.Domain.Billing;

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
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingSpace> BookingSpaces => Set<BookingSpace>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();

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

        // Booking configuration with tenant query filter
        var booking = modelBuilder.Entity<Booking>();

        booking.ToTable("Bookings");

        booking.HasKey(b => b.Id);

        booking.Property(b => b.CompanyId)
            .IsRequired();

        booking.Property(b => b.ClientId)
            .IsRequired();

        booking.Property(b => b.Title)
            .IsRequired()
            .HasMaxLength(200);

        booking.Property(b => b.StartUtc)
            .IsRequired();

        booking.Property(b => b.EndUtc)
            .IsRequired();

        booking.Property(b => b.AttendeeCount)
            .IsRequired();

        booking.Property(b => b.CreatedUtc)
            .IsRequired();

        booking.Property(b => b.IsCancelled)
            .IsRequired();

        booking.Property(b => b.CancelledUtc)
            .IsRequired(false);

        booking.Property(b => b.CancelReason)
            .IsRequired(false)
            .HasMaxLength(500);

        booking.Property(b => b.TotalAmount)
            .IsRequired()
            .HasPrecision(18, 2);

        booking.Property(b => b.SpaceConfigurationId)
            .IsRequired(false);

        booking.Property(b => b.Status)
            .IsRequired();


        booking.Property(b => b.CreatedByUserId)
            .IsRequired(false);

        booking.Property(b => b.ConfirmedByUserId)
            .IsRequired(false);

        booking.Property(b => b.CancelledByUserId)
            .IsRequired(false);

        booking.HasIndex(b => new { b.CompanyId, b.StartUtc });
        booking.HasIndex(b => new { b.CompanyId, b.EndUtc });
        booking.HasIndex(b => new { b.CompanyId, b.SpaceConfigurationId });

        // Tenant query filter for Booking
        booking.HasQueryFilter(b => companyId != null && b.CompanyId == companyId);

        // BookingSpace join table configuration
        var bookingSpace = modelBuilder.Entity<BookingSpace>();

        bookingSpace.ToTable("BookingSpaces");

        // Composite key
        bookingSpace.HasKey(x => new { x.BookingId, x.SpaceId });

        bookingSpace.Property(x => x.BookingId)
            .IsRequired();

        bookingSpace.Property(x => x.SpaceId)
            .IsRequired();

        // Foreign key to Bookings with Cascade delete
        bookingSpace.HasOne<Booking>()
            .WithMany()
            .HasForeignKey(x => x.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        // Foreign key to Spaces with Restrict delete
        bookingSpace.HasOne<Space>()
            .WithMany()
            .HasForeignKey(x => x.SpaceId)
            .OnDelete(DeleteBehavior.Restrict);

        // Index on BookingId for fast lookups
        bookingSpace.HasIndex(x => x.BookingId);

        // Index on SpaceId for fast lookups (useful for conflict detection)
        bookingSpace.HasIndex(x => x.SpaceId);

        // Invoice configuration with tenant query filter
        var invoice = modelBuilder.Entity<Invoice>();

        invoice.ToTable("Invoices");

        invoice.HasKey(i => i.Id);

        invoice.Property(i => i.CompanyId)
            .IsRequired();

        invoice.Property(i => i.BookingId)
            .IsRequired();

        invoice.Property(i => i.CreatedUtc)
            .IsRequired();

        invoice.Property(i => i.CreatedByUserId)
            .IsRequired();

        invoice.Property(i => i.SubtotalAmount)
            .IsRequired()
            .HasPrecision(18, 2);

        invoice.Property(i => i.Currency)
            .IsRequired()
            .HasMaxLength(3);

        invoice.Property(i => i.Status)
            .IsRequired()
            .HasMaxLength(20);

        // Unique index: one invoice per booking per tenant
        invoice.HasIndex(i => new { i.CompanyId, i.BookingId })
            .IsUnique();

        invoice.HasIndex(i => new { i.CompanyId, i.CreatedUtc });

        // Tenant query filter for Invoice
        invoice.HasQueryFilter(i => companyId != null && i.CompanyId == companyId);

        // InvoiceItem configuration with tenant query filter
        var invoiceItem = modelBuilder.Entity<InvoiceItem>();

        invoiceItem.ToTable("InvoiceItems");

        invoiceItem.HasKey(ii => ii.Id);

        invoiceItem.Property(ii => ii.CompanyId)
            .IsRequired();

        invoiceItem.Property(ii => ii.InvoiceId)
            .IsRequired();

        invoiceItem.Property(ii => ii.Description)
            .IsRequired()
            .HasMaxLength(200);

        invoiceItem.Property(ii => ii.Quantity)
            .IsRequired();

        invoiceItem.Property(ii => ii.UnitPrice)
            .IsRequired()
            .HasPrecision(18, 2);

        invoiceItem.Property(ii => ii.LineTotal)
            .IsRequired()
            .HasPrecision(18, 2);

        // Foreign key to Invoices with Cascade delete
        invoiceItem.HasOne<Invoice>()
            .WithMany()
            .HasForeignKey(ii => ii.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        invoiceItem.HasIndex(ii => ii.InvoiceId);

        // Tenant query filter for InvoiceItem
        invoiceItem.HasQueryFilter(ii => companyId != null && ii.CompanyId == companyId);
    }
}

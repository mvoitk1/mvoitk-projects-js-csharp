using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Sales.Domain;

namespace Sales.Infrastructure;

public class SalesDbContext(DbContextOptions<SalesDbContext> options) : DbContext(options)
{
    public const string SchemaName = "sales";

    public DbSet<Cart> Carts { get; set; } = default!;
    public DbSet<CartItem> CartItems { get; set; } = default!;
    public DbSet<Order> Orders { get; set; } = default!;
    public DbSet<OrderItem> OrderItems { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema(SchemaName);
        base.OnModelCreating(builder);

        ConfigureDateTimeAsUtc(builder);

        // Index AppUserId for the typical "find my active cart / my orders" queries.
        builder.Entity<Cart>().HasIndex(c => c.AppUserId);
        builder.Entity<Order>().HasIndex(o => o.AppUserId);

        // Index variant id to make admin "what's in this variant?" lookups cheap even
        // without an FK to catalog.
        builder.Entity<CartItem>().HasIndex(c => c.ProductVariantId);
        builder.Entity<OrderItem>().HasIndex(o => o.ProductVariantId);

        foreach (var relationship in builder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
        {
            relationship.DeleteBehavior = DeleteBehavior.Restrict;
        }
    }

    private static void ConfigureDateTimeAsUtc(ModelBuilder builder)
    {
        var dateTimeConverter = new ValueConverter<DateTime, DateTime>(
            v => v.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(v, DateTimeKind.Utc)
                : v.ToUniversalTime(),
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        var nullableDateTimeConverter = new ValueConverter<DateTime?, DateTime?>(
            v => v.HasValue
                ? (v.Value.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc)
                    : v.Value.ToUniversalTime())
                : v,
            v => v.HasValue
                ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc)
                : v);

        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime))
                    property.SetValueConverter(dateTimeConverter);
                else if (property.ClrType == typeof(DateTime?))
                    property.SetValueConverter(nullableDateTimeConverter);
            }
        }
    }
}

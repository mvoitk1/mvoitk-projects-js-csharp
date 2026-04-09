using System.Linq.Expressions;
using com.akaver.Domain.Base.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;

namespace com.akaver.DAL.Base.EF;

public class BaseDbContext<TUser, TRole, TUserRole, TUserClaim, TUserLogin, TRoleClaim, TUserToken> :
    BaseDbContext<Guid, TUser, TRole, TUserRole, TUserClaim, TUserLogin, TRoleClaim, TUserToken>
    where TUser : BaseUser<TUserRole>
    where TRole : BaseRole<TUserRole>
    where TUserRole : BaseUserRole<TUser, TRole>
    where TUserClaim : IdentityUserClaim<Guid>
    where TUserLogin : IdentityUserLogin<Guid>
    where TRoleClaim : IdentityRoleClaim<Guid>
    where TUserToken : IdentityUserToken<Guid>
{
    public BaseDbContext(DbContextOptions options)
        : base(options)
    {
    }
}

public class BaseDbContext<TKey, TUser, TRole, TUserRole, TUserClaim, TUserLogin, TRoleClaim, TUserToken> :
    IdentityDbContext<TUser, TRole, TKey, TUserClaim, TUserRole, TUserLogin, TRoleClaim, TUserToken>, IDataProtectionKeyContext
    where TKey : IEquatable<TKey>
    where TUser : BaseUser<TKey, TUserRole>
    where TRole : BaseRole<TKey, TUserRole>
    where TUserRole : BaseUserRole<TKey, TUser, TRole>
    where TUserClaim : IdentityUserClaim<TKey>
    where TUserLogin : IdentityUserLogin<TKey>
    where TRoleClaim : IdentityRoleClaim<TKey>
    where TUserToken : IdentityUserToken<TKey>
{
    // This maps to the table that stores data protection keys.
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = default!;

    
    public BaseDbContext(DbContextOptions options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        
        // Configure all DateTime properties to use UTC
        ConfigureDateTimeAsUtc(builder);
        
        // disable cascade delete
        foreach (var relationship in builder.Model
                     .GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
        {
            relationship.DeleteBehavior = DeleteBehavior.Restrict;
        }

        builder.Entity<TUserRole>().HasOne<TUser>(x => x.User!)
            .WithMany(x => x.UserRoles!)
            .HasForeignKey(x => x.UserId);

        builder.Entity<TUserRole>().HasOne<TRole>(x => x.Role!)
            .WithMany(x => x.UserRoles!)
            .HasForeignKey(x => x.RoleId);
    }
    
    /// <summary>
    /// Configures all DateTime and DateTime? properties to convert to UTC when saving to PostgreSQL.
    /// PostgreSQL's 'timestamp with time zone' type requires UTC values.
    /// </summary>
    private static void ConfigureDateTimeAsUtc(ModelBuilder builder)
    {
        // Value converter for DateTime
        var dateTimeConverter = new ValueConverter<DateTime, DateTime>(
            v => v.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(v, DateTimeKind.Utc)
                : v.ToUniversalTime(),
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        // Value converter for DateTime?
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
                {
                    property.SetValueConverter(dateTimeConverter);
                }
                else if (property.ClrType == typeof(DateTime?))
                {
                    property.SetValueConverter(nullableDateTimeConverter);
                }
            }
        }
    }
}
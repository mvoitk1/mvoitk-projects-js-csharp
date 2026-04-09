using System;
using System.Linq;
using App.Domain.ApiKey;
using App.Domain.ApiKey.SimpleList;
using App.Domain.Identity;
using App.Domain.Todo;
using com.akaver.DAL.Base.EF;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace App.DAL.EF;

public class AppDbContext: BaseDbContext<AppUser, AppRole, AppUserRole, IdentityUserClaim<Guid>,
    IdentityUserLogin<Guid>, IdentityRoleClaim<Guid>, IdentityUserToken<Guid>>
{
    // identity tokens
    public DbSet<RefreshToken> RefreshTokens { get; set; } = default!;
        
    // identity based stuff
    public DbSet<TodoCategory> TodoCategories { get; set; } = default!;
    public DbSet<TodoPriority> TodoPriorities { get; set; } = default!;
    public DbSet<TodoTask> TodoTasks { get; set; } = default!;
        
    // api key based stuff
    public DbSet<ApiKey> ApiKeys { get; set; } = default!;
    public DbSet<ListItem> ListItems { get; set; } = default!;

        
    public AppDbContext(DbContextOptions options) : base(options)
    {
    }
}


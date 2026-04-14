using Microsoft.AspNetCore.Identity;

namespace com.akaver.Domain.Base.Identity;

public class BaseUserRole<TUser, TRole> : BaseUserRole<Guid, TUser, TRole>
    where TUser : IdentityUser<Guid>
    where TRole : IdentityRole<Guid>
{
}

public class BaseUserRole<TKey, TUser, TRole> : IdentityUserRole<TKey>
    where TKey : IEquatable<TKey>
    where TUser : IdentityUser<TKey>
    where TRole : IdentityRole<TKey>
{
    public virtual TUser? User { get; set; }

    public virtual TRole? Role { get; set; }
}
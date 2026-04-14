using System.ComponentModel.DataAnnotations;
using com.akaver.Contracts.Domain.Base;
using Microsoft.AspNetCore.Identity;

namespace com.akaver.Domain.Base.Identity;

public class BaseRole<TUserRole> : 
    BaseRole<Guid, TUserRole>,
    IDomainEntityId,
    IDomainEntityId<Guid>
    where TUserRole : IdentityUserRole<Guid>
{
    public BaseRole()
    {
    }

    public BaseRole(string roleName)
        : base(roleName)
    {
    }
}

public class BaseRole<TKey, TUserRole> : IdentityRole<TKey>, IDomainEntityId<TKey>
    where TKey : IEquatable<TKey>
    where TUserRole : IdentityUserRole<TKey>
{
    public BaseRole()
    {
    }

    public BaseRole(string roleName)
        : base(roleName)
    {
    }

    [MaxLength(128)]
    public string DisplayName { get; set; } = default!;

    public virtual ICollection<TUserRole>? UserRoles { get; set; }
}
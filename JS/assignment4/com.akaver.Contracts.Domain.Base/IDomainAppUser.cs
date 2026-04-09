using Microsoft.AspNetCore.Identity;

namespace com.akaver.Contracts.Domain.Base;

public interface IDomainAppUser<TAppUser> : 
    IDomainAppUser<Guid, TAppUser>,
    IDomainAppUserId
    where TAppUser : IdentityUser<Guid>
{
}

public interface IDomainAppUser<TKey, TAppUser> : IDomainAppUserId<TKey>
    where TKey : IEquatable<TKey>
    where TAppUser : IdentityUser<TKey>
{
    TAppUser? AppUser { get; set; }
}
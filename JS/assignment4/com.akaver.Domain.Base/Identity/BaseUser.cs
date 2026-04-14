using System.ComponentModel.DataAnnotations;
using com.akaver.Contracts.Domain.Base;
using Microsoft.AspNetCore.Identity;

namespace com.akaver.Domain.Base.Identity;

public class BaseUser<TUserRole> : 
    BaseUser<Guid, TUserRole>,
    IDomainEntityId,
    IDomainEntityId<Guid>
    where TUserRole : IdentityUserRole<Guid>
{
}

public class BaseUser<TKey, TUserRole> : IdentityUser<TKey>, IDomainEntityId<TKey>
    where TKey : IEquatable<TKey>
    where TUserRole : IdentityUserRole<TKey>
{
//    [Display(Name = "FirstName", ResourceType = typeof (BaseUser))]
    [MaxLength(128)]
    public virtual string FirstName { get; set; } = default!;

  //  [Display(Name = "LastName", ResourceType = typeof (BaseUser))]
  [MaxLength(128)]
  public virtual string LastName { get; set; } = default!;

    public virtual ICollection<TUserRole>? UserRoles { get; set; }

    public virtual string FullName => this.FirstName + " " + this.LastName;

    public virtual string FullNameEmail => this.FullName + " (" + this.Email + ")";

    //[Display(Name = "FirstLastName", ResourceType = typeof (BaseUser))]
    public virtual string FirstLastName => this.FirstName + " " + this.LastName;

    //[Display(Name = "LastFirstName", ResourceType = typeof (BaseUser))]
    public virtual string LastFirstName => this.LastName + " " + this.FirstName;
}
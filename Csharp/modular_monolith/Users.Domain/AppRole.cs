using Base.Contracts;
using Microsoft.AspNetCore.Identity;

namespace Users.Domain;

public class AppRole : IdentityRole<Guid>, IDomainEntityId
{
}

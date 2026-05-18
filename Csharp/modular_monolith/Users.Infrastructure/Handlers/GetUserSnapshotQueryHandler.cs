using MediatR;
using Microsoft.AspNetCore.Identity;
using Modules.Contracts.Users.Queries;
using Users.Domain;

namespace Users.Infrastructure.Handlers;

public sealed class GetUserSnapshotQueryHandler(UserManager<AppUser> userManager)
    : IRequestHandler<GetUserSnapshotQuery, UserSnapshotDto?>
{
    public async Task<UserSnapshotDto?> Handle(GetUserSnapshotQuery request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.UserId.ToString());
        return user is null
            ? null
            : new UserSnapshotDto(user.Id, user.FirstName, user.LastName, user.Email ?? string.Empty);
    }
}

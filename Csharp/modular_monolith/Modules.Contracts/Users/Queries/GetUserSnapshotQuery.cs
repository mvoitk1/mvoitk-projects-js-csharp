using MediatR;

namespace Modules.Contracts.Users.Queries;

public sealed record GetUserSnapshotQuery(Guid UserId) : IRequest<UserSnapshotDto?>;

public sealed record UserSnapshotDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email);

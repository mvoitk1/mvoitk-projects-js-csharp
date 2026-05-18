using MediatR;

namespace Modules.Contracts.Users.Events;

/// <summary>
/// Published by the Users module after a new user record is committed.
/// Other modules (e.g. Sales) subscribe to provision per-user state.
/// </summary>
public sealed record UserRegisteredEvent(Guid UserId, string Email) : INotification;

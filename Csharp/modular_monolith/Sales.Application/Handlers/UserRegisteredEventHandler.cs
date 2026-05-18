using MediatR;
using Modules.Contracts.Users.Events;
using Sales.Application.Contracts;
using Sales.Domain;

namespace Sales.Application.Handlers;

/// <summary>
/// When a user registers, eagerly create an empty Active cart for them so the
/// "get my cart" path always has a row to return.
/// </summary>
public sealed class UserRegisteredEventHandler(ISalesUnitOfWork uow)
    : INotificationHandler<UserRegisteredEvent>
{
    public async Task Handle(UserRegisteredEvent notification, CancellationToken cancellationToken)
    {
        var existing = await uow.Carts.GetActiveCartForUserAsync(notification.UserId);
        if (existing != null) return;

        uow.Carts.Add(new Cart { AppUserId = notification.UserId });
        await uow.SaveChangesAsync();
    }
}

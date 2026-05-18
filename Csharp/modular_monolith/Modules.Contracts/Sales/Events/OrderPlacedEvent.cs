using MediatR;

namespace Modules.Contracts.Sales.Events;

public sealed record OrderPlacedEvent(
    Guid OrderId,
    string OrderNumber,
    Guid AppUserId,
    decimal TotalAmount,
    IReadOnlyCollection<OrderPlacedLine> Lines) : INotification;

public sealed record OrderPlacedLine(Guid ProductVariantId, int Quantity, decimal UnitPrice);

using MediatR;

namespace Modules.Contracts.Catalog.Commands;

/// <summary>
/// Atomically decrement stock for a set of variants. Returns success or the
/// list of variant ids that did not have enough stock; nothing is decremented
/// on failure.
/// </summary>
public sealed record ReserveStockCommand(IReadOnlyCollection<ReserveStockLine> Lines)
    : IRequest<ReserveStockResult>;

public sealed record ReserveStockLine(Guid ProductVariantId, int Quantity);

public sealed record ReserveStockResult(
    bool Success,
    IReadOnlyCollection<Guid> InsufficientStockVariantIds);

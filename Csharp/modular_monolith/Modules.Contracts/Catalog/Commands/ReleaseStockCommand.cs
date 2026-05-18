using MediatR;

namespace Modules.Contracts.Catalog.Commands;

/// <summary>
/// Compensating action for a failed order — adds stock back to the variants.
/// </summary>
public sealed record ReleaseStockCommand(IReadOnlyCollection<ReserveStockLine> Lines) : IRequest<Unit>;

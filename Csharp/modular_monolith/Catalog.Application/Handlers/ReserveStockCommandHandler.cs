using Catalog.Application.Contracts;
using MediatR;
using Modules.Contracts.Catalog.Commands;

namespace Catalog.Application.Handlers;

/// <summary>
/// Stock reservation via per-line atomic guarded decrements. Each line is
/// decremented only if enough active stock remains (a single conditional UPDATE,
/// safe under concurrency). If any line is short, returns Success=false with the
/// offending variant ids; the caller's transaction rolls back any decrements
/// already applied, so nothing is committed on failure.
/// </summary>
public sealed class ReserveStockCommandHandler(ICatalogUnitOfWork uow)
    : IRequestHandler<ReserveStockCommand, ReserveStockResult>
{
    public async Task<ReserveStockResult> Handle(ReserveStockCommand request, CancellationToken cancellationToken)
    {
        var insufficient = new List<Guid>();

        foreach (var line in request.Lines)
        {
            var ok = await uow.ProductVariants.TryDecrementStockAsync(
                line.ProductVariantId, line.Quantity, cancellationToken);
            if (!ok)
                insufficient.Add(line.ProductVariantId);
        }

        if (insufficient.Count > 0)
            return new ReserveStockResult(false, insufficient);

        // Flushes the InMemory fallback path; no-op for the relational path
        // (ExecuteUpdate already ran within the ambient transaction).
        await uow.SaveChangesAsync();
        return new ReserveStockResult(true, Array.Empty<Guid>());
    }
}

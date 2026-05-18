using Catalog.Application.Contracts;
using MediatR;
using Modules.Contracts.Catalog.Commands;

namespace Catalog.Application.Handlers;

/// <summary>
/// Atomic stock reservation: verifies every line first, then decrements all
/// of them. If any line is short, returns Success=false with the offending
/// variant ids and writes nothing.
/// </summary>
public sealed class ReserveStockCommandHandler(ICatalogUnitOfWork uow)
    : IRequestHandler<ReserveStockCommand, ReserveStockResult>
{
    public async Task<ReserveStockResult> Handle(ReserveStockCommand request, CancellationToken cancellationToken)
    {
        var insufficient = new List<Guid>();
        var loaded = new List<(Domain.ProductVariant Variant, int RequestedQty)>();

        foreach (var line in request.Lines)
        {
            var variant = await uow.ProductVariants.FindAsync(line.ProductVariantId);
            if (variant == null || !variant.IsActive || variant.StockQty < line.Quantity)
            {
                insufficient.Add(line.ProductVariantId);
                continue;
            }
            loaded.Add((variant, line.Quantity));
        }

        if (insufficient.Count > 0)
            return new ReserveStockResult(false, insufficient);

        foreach (var (variant, qty) in loaded)
        {
            variant.StockQty -= qty;
            uow.ProductVariants.Update(variant);
        }

        await uow.SaveChangesAsync();
        return new ReserveStockResult(true, Array.Empty<Guid>());
    }
}

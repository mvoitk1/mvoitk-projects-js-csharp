using Catalog.Application.Contracts;
using MediatR;
using Modules.Contracts.Catalog.Commands;

namespace Catalog.Application.Handlers;

public sealed class ReleaseStockCommandHandler(ICatalogUnitOfWork uow)
    : IRequestHandler<ReleaseStockCommand, Unit>
{
    public async Task<Unit> Handle(ReleaseStockCommand request, CancellationToken cancellationToken)
    {
        foreach (var line in request.Lines)
        {
            var variant = await uow.ProductVariants.FindAsync(line.ProductVariantId);
            if (variant == null) continue;
            variant.StockQty += line.Quantity;
            uow.ProductVariants.Update(variant);
        }

        await uow.SaveChangesAsync();
        return Unit.Value;
    }
}

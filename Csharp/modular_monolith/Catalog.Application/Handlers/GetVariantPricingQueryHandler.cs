using Catalog.Application.Contracts;
using MediatR;
using Modules.Contracts.Catalog.Queries;
using Modules.SharedKernel;

namespace Catalog.Application.Handlers;

public sealed class GetVariantPricingQueryHandler(ICatalogUnitOfWork uow)
    : IRequestHandler<GetVariantPricingQuery, VariantPricingDto?>
{
    public async Task<VariantPricingDto?> Handle(GetVariantPricingQuery request, CancellationToken cancellationToken)
    {
        var variant = await uow.ProductVariants.GetWithColorAndSizeAsync(request.ProductVariantId);
        if (variant == null) return null;

        var product = await uow.Products.GetWithDetailsAsync(variant.ProductId);
        var productName = product?.Name.Translate() ?? string.Empty;
        var firstImageUrl = product?.Images?.OrderBy(i => i.SortOrder).FirstOrDefault()?.Url;
        var colorName = variant.Color?.Name.Translate() ?? string.Empty;
        var sizeCode = variant.Size?.SizeCode ?? string.Empty;

        return new VariantPricingDto(
            variant.Id,
            variant.Sku,
            variant.Price,
            variant.UnitPrice,
            variant.StockQty,
            variant.IsActive,
            variant.ProductId,
            productName,
            colorName,
            sizeCode,
            firstImageUrl);
    }
}

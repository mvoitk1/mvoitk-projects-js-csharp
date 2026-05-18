using Catalog.Application.Contracts;
using MediatR;
using Modules.Contracts.Catalog.Queries;
using Modules.SharedKernel;

namespace Catalog.Application.Handlers;

public sealed class GetVariantsPricingQueryHandler(ICatalogUnitOfWork uow)
    : IRequestHandler<GetVariantsPricingQuery, IReadOnlyDictionary<Guid, VariantPricingDto>>
{
    public async Task<IReadOnlyDictionary<Guid, VariantPricingDto>> Handle(
        GetVariantsPricingQuery request, CancellationToken cancellationToken)
    {
        var result = new Dictionary<Guid, VariantPricingDto>();

        foreach (var id in request.ProductVariantIds.Distinct())
        {
            var variant = await uow.ProductVariants.GetWithColorAndSizeAsync(id);
            if (variant == null) continue;

            var product = await uow.Products.GetWithDetailsAsync(variant.ProductId);
            var productName = product?.Name.Translate() ?? string.Empty;
            var firstImageUrl = product?.Images?.OrderBy(i => i.SortOrder).FirstOrDefault()?.Url;
            var colorName = variant.Color?.Name.Translate() ?? string.Empty;
            var sizeCode = variant.Size?.SizeCode ?? string.Empty;

            result[variant.Id] = new VariantPricingDto(
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

        return result;
    }
}

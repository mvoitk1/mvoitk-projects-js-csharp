using MediatR;

namespace Modules.Contracts.Catalog.Queries;

/// <summary>
/// Sales asks Catalog for a single variant's pricing + availability snapshot.
/// Returns null when the variant does not exist.
/// </summary>
public sealed record GetVariantPricingQuery(Guid ProductVariantId) : IRequest<VariantPricingDto?>;

public sealed record VariantPricingDto(
    Guid Id,
    string Sku,
    decimal Price,
    decimal UnitPrice,
    int AvailableQty,
    bool IsActive,
    Guid ProductId,
    string ProductName,
    string ColorName,
    string SizeCode,
    string? FirstImageUrl);

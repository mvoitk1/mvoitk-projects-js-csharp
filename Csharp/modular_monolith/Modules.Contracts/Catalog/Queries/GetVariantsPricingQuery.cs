using MediatR;

namespace Modules.Contracts.Catalog.Queries;

/// <summary>
/// Batched variant lookup — used when Sales needs prices for an entire cart.
/// Returned dictionary is keyed by variant id; missing variants are absent
/// from the dictionary (not represented as null entries).
/// </summary>
public sealed record GetVariantsPricingQuery(IReadOnlyCollection<Guid> ProductVariantIds)
    : IRequest<IReadOnlyDictionary<Guid, VariantPricingDto>>;

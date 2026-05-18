using MediatR;

namespace Modules.Contracts.Sales.Queries;

/// <summary>
/// Catalog (admin dashboard) asks Sales for aggregate order counts.
/// </summary>
public sealed record GetOrderStatsQuery(DateTime RecentCutoffUtc) : IRequest<OrderStatsDto>;

public sealed record OrderStatsDto(int TotalCount, int RecentCount);

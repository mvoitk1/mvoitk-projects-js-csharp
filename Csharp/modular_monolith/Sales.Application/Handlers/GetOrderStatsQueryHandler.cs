using MediatR;
using Modules.Contracts.Sales.Queries;
using Sales.Application.Contracts;

namespace Sales.Application.Handlers;

public sealed class GetOrderStatsQueryHandler(ISalesUnitOfWork uow)
    : IRequestHandler<GetOrderStatsQuery, OrderStatsDto>
{
    public async Task<OrderStatsDto> Handle(GetOrderStatsQuery request, CancellationToken cancellationToken)
    {
        var orders = await uow.Orders.AllAsync();
        var total = orders.Count();
        var recent = orders.Count(o => o.CreatedAt >= request.RecentCutoffUtc);
        return new OrderStatsDto(total, recent);
    }
}

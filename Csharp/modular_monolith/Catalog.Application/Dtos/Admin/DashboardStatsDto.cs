namespace Catalog.Application.Dtos.Admin;

public record DashboardStatsDto(int ProductCount, int OrderCount, int LowStockCount, int RecentOrderCount);

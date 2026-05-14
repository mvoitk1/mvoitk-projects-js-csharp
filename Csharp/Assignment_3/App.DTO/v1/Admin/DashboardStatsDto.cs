namespace App.DTO.v1.Admin;

public record DashboardStatsDto(int ProductCount, int OrderCount, int LowStockCount, int RecentOrderCount);

namespace KetabaPOS.Desktop.Core.Interfaces;
public class DashboardData
{
    public decimal TodaySales { get; set; }
    public int TodayTransactions { get; set; }
    public int LowStockProducts { get; set; }
    public int ActiveLoans { get; set; }
    public decimal TotalRevenue { get; set; }
    public List<ChartDataPoint> SalesLast7Days { get; set; } = new();
    public List<RecentActivity> RecentActivities { get; set; } = new();
    public List<LowStockProduct> LowStockProductList { get; set; } = new();
}
public class LowStockProduct
{
    public string Name { get; set; } = string.Empty;
    public double StockQuantity { get; set; }
    public double MinStockLevel { get; set; }
}
public class ChartDataPoint
{
    public string Label { get; set; } = string.Empty;
    public decimal Value { get; set; }
}
public class RecentActivity
{
    public string Description { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string? Icon { get; set; }
}
public interface IDashboardService
{
    Task<DashboardData> GetDashboardDataAsync();
}

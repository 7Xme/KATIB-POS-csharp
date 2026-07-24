using KetabaPOS.Desktop.Core.Enums;
using KetabaPOS.Desktop.Core.Interfaces;
using KetabaPOS.Desktop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
namespace KetabaPOS.Desktop.Infrastructure.Services;
public class DashboardService : IDashboardService
{
    private readonly AppDbContext _context;
    public DashboardService(AppDbContext context) { _context = context; }
    public async Task<DashboardData> GetDashboardDataAsync()
    {
        var todayStart = DateTime.UtcNow.Date;
        var todaySales = await _context.Sales.Where(s => s.CreatedAt >= todayStart && s.Status == SaleStatus.Completed).SumAsync(s => s.TotalAmount);
        var todayTransactions = await _context.Sales.Where(s => s.CreatedAt >= todayStart).CountAsync();
        var lowStockCount = await _context.Products.Where(p => p.StockQuantity <= p.MinStockLevel).CountAsync();
        var activeLoans = await _context.Loans.Where(l => l.Status == LoanStatus.Active).CountAsync();
        var totalRevenue = await _context.Sales.Where(s => s.Status == SaleStatus.Completed).SumAsync(s => s.TotalAmount);
        var last7Days = Enumerable.Range(0, 7).Select(i => { var d = DateTime.UtcNow.Date.AddDays(-6 + i); return new ChartDataPoint { Label = d.ToString("ddd"), Value = 0 }; }).ToList();
        var salesData = await _context.Sales.Where(s => s.CreatedAt >= DateTime.UtcNow.Date.AddDays(-6) && s.Status == SaleStatus.Completed).GroupBy(s => s.CreatedAt.Date).Select(g => new { Date = g.Key, Total = g.Sum(s => s.TotalAmount) }).ToListAsync();
        foreach (var data in salesData) { var p = last7Days.FirstOrDefault(d => d.Label == data.Date.ToString("ddd")); if (p != null) p.Value = data.Total; }
            var recentSales = await _context.Sales.Include(s => s.User).OrderByDescending(s => s.CreatedAt).Take(10).ToListAsync();
            var activities = recentSales.Select(s => new RecentActivity { Description = $"Sale #{s.InvoiceNumber} - {s.TotalAmount:F2} by {s.User?.DisplayName ?? "N/A"}", Timestamp = s.CreatedAt, Icon = "Receipt" }).ToList();
            var lowStockProducts = await _context.Products.Where(p => p.StockQuantity <= p.MinStockLevel && p.IsActive).OrderBy(p => p.StockQuantity).Take(10).Select(p => new LowStockProduct { Name = p.Name, StockQuantity = p.StockQuantity, MinStockLevel = p.MinStockLevel }).ToListAsync();
            return new DashboardData { TodaySales = todaySales, TodayTransactions = todayTransactions, LowStockProducts = lowStockCount, ActiveLoans = activeLoans, TotalRevenue = totalRevenue, SalesLast7Days = last7Days, RecentActivities = activities, LowStockProductList = lowStockProducts };
    }
}

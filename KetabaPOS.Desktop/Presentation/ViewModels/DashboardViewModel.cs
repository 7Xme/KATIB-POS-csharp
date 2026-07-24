using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KetabaPOS.Desktop.Core.Interfaces;

namespace KetabaPOS.Desktop.Presentation.ViewModels;

public record ChartDataPoint(string Label, decimal Value);
public record RecentActivity(string Description, DateTime Timestamp);

public partial class DashboardViewModel : ObservableObject
{
    private readonly IDashboardService _dashboardService;
    [ObservableProperty] private decimal _todaySales;
    [ObservableProperty] private int _todayTransactions;
    [ObservableProperty] private int _lowStockProducts;
    [ObservableProperty] private int _activeLoans;
    [ObservableProperty] private decimal _totalRevenue;
    [ObservableProperty] private List<ChartDataPoint> _salesLast7Days = new();
    [ObservableProperty] private List<RecentActivity> _recentActivities = new();
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _errorMessage = string.Empty;

    public DashboardViewModel(IDashboardService dashboardService) { _dashboardService = dashboardService; }

    [RelayCommand]
    private async Task LoadDashboardAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        try
        {
            var data = await _dashboardService.GetDashboardDataAsync();
            TodaySales = data.TodaySales;
            TodayTransactions = data.TodayTransactions;
            LowStockProducts = data.LowStockProducts;
            ActiveLoans = data.ActiveLoans;
            TotalRevenue = data.TotalRevenue;
            SalesLast7Days = data.SalesLast7Days;
            RecentActivities = data.RecentActivities;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load dashboard: {ex.Message}";
        }
        finally { IsLoading = false; }
    }
}

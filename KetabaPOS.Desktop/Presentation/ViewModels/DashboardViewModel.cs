using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KetabaPOS.Desktop.Core.Interfaces;

namespace KetabaPOS.Desktop.Presentation.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly IDashboardService _dashboardService;

    [ObservableProperty]
    private decimal _todaySales;

    [ObservableProperty]
    private int _todayTransactions;

    [ObservableProperty]
    private int _lowStockProducts;

    [ObservableProperty]
    private int _activeLoans;

    [ObservableProperty]
    private decimal _totalRevenue;

    [ObservableProperty]
    private List<ChartDataPoint> _salesLast7Days = new();

    [ObservableProperty]
    private List<RecentActivity> _recentActivities = new();

    [ObservableProperty]
    private bool _isLoading;

    public DashboardViewModel(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [RelayCommand]
    private async Task LoadDashboardAsync()
    {
        IsLoading = true;
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
        finally
        {
            IsLoading = false;
        }
    }
}

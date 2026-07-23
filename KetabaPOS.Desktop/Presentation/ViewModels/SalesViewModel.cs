using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KetabaPOS.Desktop.Core.Interfaces;
using KetabaPOS.Desktop.Core.Models;
namespace KetabaPOS.Desktop.Presentation.ViewModels;
public partial class SalesViewModel : ObservableObject
{
    private readonly ISaleService _saleService;
    [ObservableProperty] private ObservableCollection<Sale> _sales = new();
    [ObservableProperty] private DateTime? _fromDate;
    [ObservableProperty] private DateTime? _toDate;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private Sale? _selectedSale;
    public SalesViewModel(ISaleService saleService) { _saleService = saleService; }
    [RelayCommand] private async Task LoadSalesAsync() { IsLoading = true; try { Sales = new ObservableCollection<Sale>(await _saleService.GetSalesAsync(FromDate, ToDate)); } finally { IsLoading = false; } }
    [RelayCommand] private async Task ViewSaleDetailAsync(Sale sale) { SelectedSale = await _saleService.GetSaleByIdAsync(sale.Id); }
    [RelayCommand] private async Task PrintReceiptAsync(Sale sale) { var r = await _saleService.GenerateReceiptAsync(sale.Id); var t = System.Text.Encoding.UTF8.GetString(r); var w = new System.Windows.Window { Title = $"Receipt - {sale.InvoiceNumber}", Width = 400, Height = 500, WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner }; w.ShowDialog(); }
}

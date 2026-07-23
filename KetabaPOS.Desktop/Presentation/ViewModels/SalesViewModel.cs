using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KetabaPOS.Desktop.Core.Interfaces;
using KetabaPOS.Desktop.Core.Models;

namespace KetabaPOS.Desktop.Presentation.ViewModels;

public partial class SalesViewModel : ObservableObject
{
    private readonly ISaleService _saleService;

    [ObservableProperty]
    private ObservableCollection<Sale> _sales = new();

    [ObservableProperty]
    private DateTime? _fromDate;

    [ObservableProperty]
    private DateTime? _toDate;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private Sale? _selectedSale;

    public SalesViewModel(ISaleService saleService)
    {
        _saleService = saleService;
    }

    [RelayCommand]
    private async Task LoadSalesAsync()
    {
        IsLoading = true;
        try
        {
            var sales = await _saleService.GetSalesAsync(FromDate, ToDate);
            Sales = new ObservableCollection<Sale>(sales);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ViewSaleDetailAsync(Sale sale)
    {
        SelectedSale = await _saleService.GetSaleByIdAsync(sale.Id);
    }

    [RelayCommand]
    private async Task PrintReceiptAsync(Sale sale)
    {
        var receipt = await _saleService.GenerateReceiptAsync(sale.Id);
        var receiptText = System.Text.Encoding.UTF8.GetString(receipt);

        var dialog = new System.Windows.Controls.TextBox
        {
            Text = receiptText,
            IsReadOnly = true,
            FontFamily = new System.Windows.Media.FontFamily("Consolas"),
            TextWrapping = System.Windows.TextWraping.NoWrap,
            VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto,
            Height = 400
        };

        var window = new System.Windows.Window
        {
            Title = $"Receipt - {sale.InvoiceNumber}",
            Content = dialog,
            Width = 400,
            Height = 500,
            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner
        };
        window.ShowDialog();
    }
}

using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KetabaPOS.Desktop.Core.Enums;
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
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _pageSize = 20;
    [ObservableProperty] private int _totalCount;
    public int TotalPages => (int)System.Math.Ceiling((double)TotalCount / PageSize);

    [ObservableProperty] private Sale? _selectedSale;
    [ObservableProperty] private ObservableCollection<SaleItem> _saleItems = new();
    [ObservableProperty] private bool _showDetail;

    public SalesViewModel(ISaleService saleService) { _saleService = saleService; }

    [RelayCommand]
    private async Task LoadSalesAsync()
    {
        IsLoading = true;
        try
        {
            var allSales = await _saleService.GetSalesAsync(FromDate, ToDate);
            Sales = new ObservableCollection<Sale>(allSales);
            TotalCount = allSales.Count();
            OnPropertyChanged(nameof(TotalPages));
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        CurrentPage = 1;
        await LoadSalesAsync();
    }

    [RelayCommand]
    private async Task NextPageAsync()
    {
        if (CurrentPage < TotalPages) { CurrentPage++; await LoadSalesAsync(); }
    }

    [RelayCommand]
    private async Task PreviousPageAsync()
    {
        if (CurrentPage > 1) { CurrentPage--; await LoadSalesAsync(); }
    }

    [RelayCommand]
    private async Task ViewSaleDetailAsync(Sale sale)
    {
        SelectedSale = await _saleService.GetSaleByIdAsync(sale.Id);
        if (SelectedSale != null)
        {
            SaleItems = new ObservableCollection<SaleItem>(SelectedSale.SaleItems);
            ShowDetail = true;
        }
    }

    [RelayCommand]
    private void CloseDetail()
    {
        ShowDetail = false;
        SelectedSale = null;
        SaleItems.Clear();
    }

    [RelayCommand]
    private async Task CancelSaleAsync(Sale sale)
    {
        var result = MessageBox.Show($"Cancel sale {sale.InvoiceNumber}?\nThis will restore stock quantities.",
            "Confirm Cancel", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result == MessageBoxResult.Yes)
        {
            await _saleService.CancelSaleAsync(sale.Id);
            await LoadSalesAsync();
        }
    }

    [RelayCommand]
    private async Task PrintReceiptAsync(Sale sale)
    {
        var receiptBytes = await _saleService.GenerateReceiptAsync(sale.Id);
        var receiptText = System.Text.Encoding.UTF8.GetString(receiptBytes);

        var textBox = new TextBox
        {
            Text = receiptText,
            IsReadOnly = true,
            FontFamily = new System.Windows.Media.FontFamily("Consolas"),
            FontSize = 12,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            TextWrapping = TextWrapping.NoWrap,
            Margin = new Thickness(10)
        };

        var printButton = new Button
        {
            Content = "Print",
            Margin = new Thickness(10, 0, 10, 10),
            Height = 36,
            FontSize = 14,
            Padding = new Thickness(16, 0, 16, 0)
        };

        printButton.Click += (s, e) =>
        {
            var dlg = new PrintDialog();
            if (dlg.ShowDialog() == true)
            {
                var flowDoc = new FlowDocument(new Paragraph(new Run(receiptText)));
                flowDoc.FontFamily = new System.Windows.Media.FontFamily("Consolas");
                flowDoc.FontSize = 12;
                dlg.PrintDocument(((IDocumentPaginatorSource)flowDoc).DocumentPaginator, $"Receipt - {sale.InvoiceNumber}");
            }
        };

        var stack = new StackPanel();
        stack.Children.Add(textBox);
        stack.Children.Add(printButton);

        var window = new Window
        {
            Title = $"Receipt - {sale.InvoiceNumber}",
            Content = stack,
            Width = 420,
            Height = 600,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ResizeMode = ResizeMode.CanResize
        };
        window.ShowDialog();
    }
}

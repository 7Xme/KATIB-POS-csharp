using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KetabaPOS.Desktop.Core.Interfaces;
using KetabaPOS.Desktop.Core.Models;

namespace KetabaPOS.Desktop.Presentation.ViewModels;

public partial class SalesViewModel : ObservableObject
{
    private readonly ISaleService _saleService;
    private readonly IReceiptService _receiptService;

    [ObservableProperty] private ObservableCollection<Sale> _sales = new();
    [ObservableProperty] private DateTime? _fromDate;
    [ObservableProperty] private DateTime? _toDate;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _pageSize = 20;
    [ObservableProperty] private int _totalCount;
    public int TotalPages => (int)System.Math.Ceiling((double)TotalCount / PageSize);

    [ObservableProperty] private int _summaryTransactions;
    [ObservableProperty] private decimal _summaryTotalSales;
    [ObservableProperty] private decimal _summaryTotalTax;
    [ObservableProperty] private decimal _summaryTotalDiscounts;
    [ObservableProperty] private decimal _summaryAverage;

    [ObservableProperty] private Sale? _selectedSale;
    [ObservableProperty] private ObservableCollection<SaleItem> _saleItems = new();
    [ObservableProperty] private bool _showDetail;

    public SalesViewModel(ISaleService saleService, IReceiptService receiptService) { _saleService = saleService; _receiptService = receiptService; }

    [RelayCommand]
    private async Task LoadSalesAsync()
    {
        IsLoading = true;
        StatusMessage = string.Empty;
        try
        {
            Sales = new ObservableCollection<Sale>(await _saleService.GetSalesAsync(FromDate, ToDate, CurrentPage, PageSize));
            TotalCount = (await _saleService.GetSalesAsync(FromDate, ToDate)).Count();
            OnPropertyChanged(nameof(TotalPages));
            var summary = await _saleService.GetSalesSummaryAsync(FromDate, ToDate);
            SummaryTransactions = summary.TotalTransactions;
            SummaryTotalSales = summary.TotalSales;
            SummaryTotalTax = summary.TotalTax;
            SummaryTotalDiscounts = summary.TotalDiscounts;
            SummaryAverage = summary.AverageTransactionValue;
        }
        catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task SearchAsync() { CurrentPage = 1; await LoadSalesAsync(); }

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
        if (sale == null) return;
        IsLoading = true;
        try
        {
            SelectedSale = await _saleService.GetSaleByIdAsync(sale.Id);
            if (SelectedSale != null)
            {
                SaleItems = new ObservableCollection<SaleItem>(SelectedSale.SaleItems ?? new List<SaleItem>());
                ShowDetail = true;
            }
        }
        catch (Exception ex) { StatusMessage = $"Error loading detail: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private void CloseDetail() { ShowDetail = false; SelectedSale = null; SaleItems.Clear(); }

    [RelayCommand]
    private async Task CancelSaleAsync(Sale sale)
    {
        if (sale == null) return;
        if (MessageBox.Show($"Cancel sale {sale.InvoiceNumber}?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        IsLoading = true;
        try
        {
            await _saleService.CancelSaleAsync(sale.Id);
            StatusMessage = "Sale cancelled.";
            await LoadSalesAsync();
        }
        catch (Exception ex) { StatusMessage = $"Cancel failed: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task RefundSaleAsync(Sale sale)
    {
        if (sale == null || sale.Status != Core.Enums.SaleStatus.Completed) return;
        var dialog = new System.Windows.Controls.TextBox { Text = "Customer return", AcceptsReturn = true, TextWrapping = System.Windows.TextWrapping.Wrap, Height = 60, Margin = new Thickness(12) };
        var okBtn = new System.Windows.Controls.Button { Content = "Confirm Refund", IsDefault = true, Margin = new Thickness(12, 0, 12, 12), Height = 36 };
        var cancelBtn = new System.Windows.Controls.Button { Content = "Cancel", IsCancel = true, Margin = new Thickness(0, 0, 12, 12), Height = 36 };
        var stack = new System.Windows.Controls.StackPanel();
        stack.Children.Add(new System.Windows.Controls.TextBlock { Text = $"Refund sale {sale.InvoiceNumber}? Stock will be returned.", Margin = new Thickness(12, 12, 12, 0), FontWeight = FontWeights.Medium });
        stack.Children.Add(dialog);
        var btnPanel = new System.Windows.Controls.StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, HorizontalAlignment = System.Windows.HorizontalAlignment.Right };
        btnPanel.Children.Add(cancelBtn); btnPanel.Children.Add(okBtn);
        stack.Children.Add(btnPanel);
        var win = new Window { Title = "Confirm Refund", Content = stack, Width = 400, Height = 220, WindowStartupLocation = WindowStartupLocation.CenterOwner, Owner = Application.Current.MainWindow, ResizeMode = ResizeMode.NoResize };
        var result = false;
        okBtn.Click += (s, e) => { result = true; win.DialogResult = true; win.Close(); };
        cancelBtn.Click += (s, e) => win.Close();
        if (win.ShowDialog() == true && result)
        {
            IsLoading = true;
            try { await _saleService.RefundSaleAsync(sale.Id, dialog.Text); StatusMessage = "Sale refunded."; await LoadSalesAsync(); }
            catch (Exception ex) { StatusMessage = $"Refund failed: {ex.Message}"; }
            finally { IsLoading = false; }
        }
    }

    [RelayCommand]
    private async Task PrintReceiptAsync(Sale sale)
    {
        if (sale == null) return;
        IsLoading = true;
        try
        {
            var fullSale = await _saleService.GetSaleByIdAsync(sale.Id);
            if (fullSale == null) { StatusMessage = "Sale not found."; return; }
            var doc = await _receiptService.BuildReceiptDocumentAsync(fullSale);
            var win = new Views.ReceiptPreviewWindow(_receiptService, fullSale, doc)
            {
                Owner = Application.Current.MainWindow
            };
            win.ShowDialog();
        }
        catch (Exception ex) { StatusMessage = $"Receipt error: {ex.Message}"; }
        finally { IsLoading = false; }
    }
}

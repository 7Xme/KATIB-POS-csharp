using System.Collections.ObjectModel;
using System.IO;
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
    [ObservableProperty] private ObservableCollection<Sale> _sales = new();
    [ObservableProperty] private DateTime? _fromDate;
    [ObservableProperty] private DateTime? _toDate;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private Sale? _selectedSale;

    public SalesViewModel(ISaleService saleService) { _saleService = saleService; }

    [RelayCommand]
    private async Task LoadSalesAsync()
    {
        IsLoading = true;
        try { Sales = new ObservableCollection<Sale>(await _saleService.GetSalesAsync(FromDate, ToDate)); }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task ViewSaleDetailAsync(Sale sale)
    {
        SelectedSale = await _saleService.GetSaleByIdAsync(sale.Id);
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
            Content = "🖨️ Print",
            Margin = new Thickness(10, 0, 10, 10),
            Height = 36,
            FontSize = 14
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

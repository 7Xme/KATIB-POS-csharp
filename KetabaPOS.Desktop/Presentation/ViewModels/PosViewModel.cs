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

public partial class CartItemViewModel : ObservableObject
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    [ObservableProperty] private double _quantity = 1;
    [ObservableProperty] private decimal _unitPrice;
    [ObservableProperty] private decimal _discountAmount;
    public decimal Total => (decimal)((double)UnitPrice * Quantity) - DiscountAmount;
    partial void OnQuantityChanged(double value) => OnPropertyChanged(nameof(Total));
    partial void OnUnitPriceChanged(decimal value) => OnPropertyChanged(nameof(Total));
    partial void OnDiscountAmountChanged(decimal value) => OnPropertyChanged(nameof(Total));
}

public partial class PosViewModel : ObservableObject
{
    private readonly IProductService _productService;
    private readonly ISaleService _saleService;
    private int _lastSaleId;

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private int? _selectedCategoryId;
    [ObservableProperty] private ObservableCollection<Product> _products = new();
    [ObservableProperty] private ObservableCollection<CartItemViewModel> _cartItems = new();
    [ObservableProperty] private ObservableCollection<Category> _categories = new();
    [ObservableProperty] private decimal _subtotal;
    [ObservableProperty] private decimal _taxAmount;
    [ObservableProperty] private decimal _discountAmount;
    [ObservableProperty] private decimal _totalAmount;
    [ObservableProperty] private decimal _paidAmount;
    [ObservableProperty] private decimal _changeAmount;
    [ObservableProperty] private PaymentMethod _selectedPaymentMethod = PaymentMethod.Cash;
    [ObservableProperty] private int? _selectedCustomerId;
    [ObservableProperty] private ObservableCollection<Customer> _customers = new();
    [ObservableProperty] private bool _showReceiptButton;

    public PosViewModel(IProductService productService, ISaleService saleService)
    {
        _productService = productService;
        _saleService = saleService;
    }

    [RelayCommand]
    private async Task LoadProductsAsync()
    {
        var products = await _productService.GetProductsAsync(SearchText, SelectedCategoryId);
        Products = new ObservableCollection<Product>(products);
    }

    [RelayCommand] private async Task SearchProductsAsync() => await LoadProductsAsync();

    [RelayCommand]
    private void AddToCart(Product product)
    {
        var existing = CartItems.FirstOrDefault(c => c.ProductId == product.Id);
        if (existing != null) existing.Quantity++;
        else CartItems.Add(new CartItemViewModel { ProductId = product.Id, ProductName = product.Name, UnitPrice = product.RetailPrice, Quantity = 1 });
        Recalculate();
    }

    [RelayCommand] private void RemoveFromCart(CartItemViewModel item) { CartItems.Remove(item); Recalculate(); }

    [RelayCommand]
    private void ClearCart() { CartItems.Clear(); Subtotal = 0; TaxAmount = 0; DiscountAmount = 0; TotalAmount = 0; PaidAmount = 0; ChangeAmount = 0; ShowReceiptButton = false; }

    [RelayCommand]
    private async Task CompleteSaleAsync()
    {
        if (CartItems.Count == 0) return;
        var sale = new Sale
        {
            CustomerId = SelectedCustomerId,
            PaymentMethod = SelectedPaymentMethod,
            Subtotal = Subtotal,
            TaxAmount = TaxAmount,
            DiscountAmount = DiscountAmount,
            TotalAmount = TotalAmount,
            PaidAmount = PaidAmount,
            ChangeAmount = System.Math.Max(0, PaidAmount - TotalAmount)
        };
        var items = CartItems.Select(c => new SaleItem
        {
            ProductId = c.ProductId,
            Quantity = c.Quantity,
            UnitPrice = c.UnitPrice,
            DiscountAmount = c.DiscountAmount
        }).ToList();

        var completedSale = await _saleService.CreateSaleAsync(sale, items);
        _lastSaleId = completedSale.Id;
        ClearCart();
        ShowReceiptButton = true;
        await LoadProductsAsync();
    }

    [RelayCommand]
    private async Task PrintReceiptAsync()
    {
        if (_lastSaleId == 0) return;
        var receiptBytes = await _saleService.GenerateReceiptAsync(_lastSaleId);
        var receiptText = System.Text.Encoding.UTF8.GetString(receiptBytes);

        var textBox = new TextBox
        {
            Text = receiptText,
            IsReadOnly = true,
            FontFamily = new System.Windows.Media.FontFamily("Consolas"),
            FontSize = 12,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
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
                dlg.PrintDocument(((IDocumentPaginatorSource)flowDoc).DocumentPaginator, "Receipt");
            }
        };

        var stack = new StackPanel();
        stack.Children.Add(textBox);
        stack.Children.Add(printButton);

        var window = new Window
        {
            Title = "Receipt",
            Content = stack,
            Width = 420,
            Height = 600,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ResizeMode = ResizeMode.CanResize
        };
        window.ShowDialog();
    }

    private void Recalculate()
    {
        Subtotal = CartItems.Sum(c => (decimal)((double)c.UnitPrice * c.Quantity));
        DiscountAmount = CartItems.Sum(c => c.DiscountAmount);
        TaxAmount = Subtotal * 0.15m;
        TotalAmount = Subtotal + TaxAmount - DiscountAmount;
        ChangeAmount = System.Math.Max(0, PaidAmount - TotalAmount);
    }

    partial void OnPaidAmountChanged(decimal value)
    {
        ChangeAmount = System.Math.Max(0, value - TotalAmount);
    }
}

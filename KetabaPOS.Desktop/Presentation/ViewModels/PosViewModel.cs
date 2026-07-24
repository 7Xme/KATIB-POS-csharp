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
    private readonly IReceiptService _receiptService;
    private readonly IAuthService _authService;
    private readonly ISettingsService _settingsService;
    private decimal _taxRate = 0.15m;
    private int _lastSaleId;
    private CancellationTokenSource? _searchCts;

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _barcodeText = string.Empty;
    [ObservableProperty] private string _scanStatus = string.Empty;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _isLoading;
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

    public PosViewModel(IProductService productService, ISaleService saleService, IReceiptService receiptService, IAuthService authService, ISettingsService settingsService)
    {
        _productService = productService;
        _saleService = saleService;
        _receiptService = receiptService;
        _authService = authService;
        _settingsService = settingsService;
        _ = LoadTaxRateAsync();
    }

    private async Task LoadTaxRateAsync()
    {
        try
        {
            var rateStr = await _settingsService.GetSettingAsync("tax_rate");
            if (decimal.TryParse(rateStr, out var rate) && rate >= 0)
                _taxRate = rate / 100m;
        }
        catch { /* use default 15% */ }
    }

    [RelayCommand]
    private async Task LoadProductsAsync()
    {
        IsLoading = true;
        StatusMessage = string.Empty;
        try
        {
            Products = new ObservableCollection<Product>(await _productService.GetProductsAsync(SearchText, SelectedCategoryId));
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading products: {ex.Message}";
        }
        finally { IsLoading = false; }
    }

    partial void OnSearchTextChanged(string value) => DebounceSearch();
    partial void OnSelectedCategoryIdChanged(int? value) => DebounceSearch();
    private void DebounceSearch()
    {
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        _ = Task.Delay(300, _searchCts.Token).ContinueWith(_ => { if (!_searchCts.Token.IsCancellationRequested) _ = LoadProductsAsync(); }, TaskScheduler.FromCurrentSynchronizationContext());
    }
    [RelayCommand]
    private async Task SearchProductsAsync() => await LoadProductsAsync();

    [RelayCommand]
    private async Task LoadCustomersAsync()
    {
        try { Customers = new ObservableCollection<Customer>(await _productService.GetCustomersAsync()); }
        catch { /* ignore */ }
    }

    [RelayCommand]
    private void AddToCart(Product product)
    {
        if (product == null) return;
        var existing = CartItems.FirstOrDefault(c => c.ProductId == product.Id);
        if (existing != null) existing.Quantity++;
        else CartItems.Add(new CartItemViewModel { ProductId = product.Id, ProductName = product.Name, UnitPrice = product.RetailPrice, Quantity = 1 });
        Recalculate();
    }

    [RelayCommand]
    private async Task ScanBarcodeAsync()
    {
        if (string.IsNullOrWhiteSpace(BarcodeText)) return;
        var barcode = BarcodeText.Trim();
        IsLoading = true;
        try
        {
            var product = await _productService.GetByBarcodeAsync(barcode);
            if (product != null)
            {
                AddToCart(product);
                ScanStatus = $"Added: {product.Name}";
            }
            else
            {
                ScanStatus = $"Not found: {barcode}";
            }
        }
        catch (Exception ex) { ScanStatus = $"Error: {ex.Message}"; }
        finally { IsLoading = false; }
        BarcodeText = string.Empty;
    }

    [RelayCommand]
    private void RemoveFromCart(CartItemViewModel item)
    {
        if (item == null) return;
        CartItems.Remove(item);
        Recalculate();
    }

    [RelayCommand]
    private void ClearCart()
    {
        CartItems.Clear();
        Subtotal = 0;
        TaxAmount = 0;
        DiscountAmount = 0;
        TotalAmount = 0;
        PaidAmount = 0;
        ChangeAmount = 0;
        ShowReceiptButton = false;
        ScanStatus = string.Empty;
    }

    [RelayCommand]
    private async Task CompleteSaleAsync()
    {
        if (CartItems.Count == 0)
        {
            StatusMessage = "Cart is empty.";
            return;
        }
        IsLoading = true;
        StatusMessage = string.Empty;
        try
        {
            var currentUser = await _authService.GetCurrentUserAsync();
            var sale = new Sale
            {
                UserId = currentUser?.Id ?? 1,
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
            if (completedSale == null)
            {
                StatusMessage = "Sale failed — no data returned.";
                return;
            }
            _lastSaleId = completedSale.Id;
            ClearCart();
            ShowReceiptButton = true;
            StatusMessage = $"Sale completed! Invoice: {completedSale.InvoiceNumber}";
            await LoadProductsAsync();
        }
        catch (Exception ex) { StatusMessage = $"Sale failed: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task PrintReceiptAsync()
    {
        if (_lastSaleId == 0) return;
        IsLoading = true;
        try
        {
            var fullSale = await _receiptService.GetSaleWithDetailsAsync(_lastSaleId);
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

    private void Recalculate()
    {
        Subtotal = CartItems.Sum(c => (decimal)((double)c.UnitPrice * c.Quantity));
        DiscountAmount = CartItems.Sum(c => c.DiscountAmount);
        TaxAmount = Subtotal * _taxRate;
        TotalAmount = Subtotal + TaxAmount - DiscountAmount;
        ChangeAmount = System.Math.Max(0, PaidAmount - TotalAmount);
    }

    partial void OnPaidAmountChanged(decimal value)
    {
        ChangeAmount = System.Math.Max(0, value - TotalAmount);
    }
}

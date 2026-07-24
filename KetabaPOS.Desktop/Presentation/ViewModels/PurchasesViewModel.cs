using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KetabaPOS.Desktop.Core.Interfaces;
using KetabaPOS.Desktop.Core.Models;
namespace KetabaPOS.Desktop.Presentation.ViewModels;
public partial class PurchasesViewModel : ObservableObject
{
    private readonly IPurchaseService _purchaseService;
    private readonly IProductService _productService;
    [ObservableProperty] private ObservableCollection<Purchase> _purchases = new();
    [ObservableProperty] private ObservableCollection<Supplier> _suppliers = new();
    [ObservableProperty] private ObservableCollection<Product> _searchProducts = new();
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private int? _filterSupplierId;
    [ObservableProperty] private bool? _filterIsReceived;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _showForm;
    [ObservableProperty] private string _formTitle = "New Purchase Order";
    [ObservableProperty] private int _formSupplierId;
    [ObservableProperty] private string _formNotes = string.Empty;
    [ObservableProperty] private decimal _formTaxAmount;
    [ObservableProperty] private decimal _formTotalAmount;
    [ObservableProperty] private string _productSearchText = string.Empty;
    [ObservableProperty] private ObservableCollection<PurchaseItemViewModel> _formItems = new();
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private int _pageSize = 20;
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public PurchasesViewModel(IPurchaseService purchaseService, IProductService productService) { _purchaseService = purchaseService; _productService = productService; }
    [RelayCommand] private async Task LoadPurchasesAsync()
    {
        IsLoading = true; StatusMessage = string.Empty;
        try
        {
            Purchases = new ObservableCollection<Purchase>(await _purchaseService.GetPurchasesAsync(SearchText, FilterSupplierId, FilterIsReceived, CurrentPage, PageSize));
            TotalCount = await _purchaseService.GetTotalCountAsync(SearchText, FilterSupplierId, FilterIsReceived);
            OnPropertyChanged(nameof(TotalPages));
        }
        catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
        finally { IsLoading = false; }
    }
    [RelayCommand] private async Task LoadSuppliersAsync()
    {
        try { Suppliers = new ObservableCollection<Supplier>(await _purchaseService.GetSuppliersAsync()); }
        catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
    }
    [RelayCommand] private async Task SearchAsync() { CurrentPage = 1; await LoadPurchasesAsync(); }
    [RelayCommand] private async Task NextPageAsync() { if (CurrentPage < TotalPages) { CurrentPage++; await LoadPurchasesAsync(); } }
    [RelayCommand] private async Task PreviousPageAsync() { if (CurrentPage > 1) { CurrentPage--; await LoadPurchasesAsync(); } }
    [RelayCommand] private async Task SearchProductsForPoAsync()
    {
        try { SearchProducts = new ObservableCollection<Product>(await _purchaseService.GetProductsAsync(ProductSearchText)); }
        catch { /* ignore */ }
    }
    [RelayCommand]
    private void AddLineToPo(Product product)
    {
        if (product == null) return;
        var existing = FormItems.FirstOrDefault(fi => fi.ProductId == product.Id);
        if (existing != null) existing.Quantity++;
        else FormItems.Add(new PurchaseItemViewModel { ProductId = product.Id, ProductName = product.Name, Quantity = 1, UnitCost = product.CostPrice });
        RecalculatePo();
    }
    [RelayCommand]
    private void RemoveLineFromPo(PurchaseItemViewModel item)
    {
        if (item == null) return;
        FormItems.Remove(item);
        RecalculatePo();
    }
    private void RecalculatePo()
    {
        FormTotalAmount = FormItems.Sum(i => (decimal)i.Quantity * i.UnitCost) + FormTaxAmount;
    }
    partial void OnFormTaxAmountChanged(decimal value) => RecalculatePo();
    [RelayCommand]
    private void ShowAddForm() { ResetForm(); ShowForm = true; FormTitle = "New Purchase Order"; }
    [RelayCommand]
    private void CancelForm() { ShowForm = false; ResetForm(); }
    [RelayCommand]
    private async Task SavePurchaseAsync()
    {
        if (FormSupplierId <= 0) { MessageBox.Show("Select a supplier.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        if (FormItems.Count == 0) { MessageBox.Show("Add at least one item.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        IsLoading = true;
        try
        {
            var purchase = new Purchase
            {
                SupplierId = FormSupplierId,
                TaxAmount = FormTaxAmount,
                Notes = FormNotes,
                IsReceived = false
            };
            var items = FormItems.Select(i => new PurchaseItem
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                UnitCost = i.UnitCost
            }).ToList();
            await _purchaseService.CreateAsync(purchase, items);
            ShowForm = false; ResetForm(); StatusMessage = "Purchase order created.";
            await LoadPurchasesAsync();
        }
        catch (Exception ex) { StatusMessage = $"Save failed: {ex.Message}"; }
        finally { IsLoading = false; }
    }
    [RelayCommand]
    private async Task ReceivePurchaseAsync(Purchase purchase)
    {
        if (purchase == null || purchase.IsReceived) return;
        if (MessageBox.Show($"Receive purchase order {purchase.PurchaseOrderNumber}? This will add stock.", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        IsLoading = true;
        try { await _purchaseService.ReceivePurchaseAsync(purchase.Id); StatusMessage = "Purchase received. Stock updated."; await LoadPurchasesAsync(); }
        catch (Exception ex) { StatusMessage = $"Receive failed: {ex.Message}"; }
        finally { IsLoading = false; }
    }
    [RelayCommand]
    private async Task CancelPurchaseAsync(Purchase purchase)
    {
        if (purchase == null) return;
        if (MessageBox.Show($"Cancel purchase {purchase.PurchaseOrderNumber}?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        IsLoading = true;
        try { await _purchaseService.CancelPurchaseAsync(purchase.Id); StatusMessage = "Purchase cancelled."; await LoadPurchasesAsync(); }
        catch (Exception ex) { StatusMessage = $"Cancel failed: {ex.Message}"; }
        finally { IsLoading = false; }
    }
    private void ResetForm()
    {
        FormSupplierId = 0; FormNotes = string.Empty; FormTaxAmount = 0; FormTotalAmount = 0;
        FormItems.Clear(); ProductSearchText = string.Empty; SearchProducts.Clear();
    }
}
public partial class PurchaseItemViewModel : ObservableObject
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    [ObservableProperty] private double _quantity = 1;
    [ObservableProperty] private decimal _unitCost;
    public decimal LineTotal => (decimal)Quantity * UnitCost;
    partial void OnQuantityChanged(double value) => OnPropertyChanged(nameof(LineTotal));
    partial void OnUnitCostChanged(decimal value) => OnPropertyChanged(nameof(LineTotal));
}

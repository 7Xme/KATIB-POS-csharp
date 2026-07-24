using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KetabaPOS.Desktop.Core.Interfaces;
using KetabaPOS.Desktop.Core.Models;
using Microsoft.Win32;

namespace KetabaPOS.Desktop.Presentation.ViewModels;

public partial class ProductsViewModel : ObservableObject
{
    private readonly IProductService _productService;

    [ObservableProperty] private ObservableCollection<Product> _products = new();
    [ObservableProperty] private ObservableCollection<Category> _categories = new();
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private int? _selectedCategoryId;
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private int _pageSize = 20;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _showForm;
    [ObservableProperty] private bool _isEditing;
    [ObservableProperty] private string _formTitle = "Add Product";

    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    [ObservableProperty] private int _formId;
    [ObservableProperty] private string _formName = string.Empty;
    [ObservableProperty] private string _formNameAr = string.Empty;
    [ObservableProperty] private string _formBarcode = string.Empty;
    [ObservableProperty] private string _formSku = string.Empty;
    [ObservableProperty] private int _formCategoryId;
    [ObservableProperty] private decimal _formCostPrice;
    [ObservableProperty] private decimal _formRetailPrice;
    [ObservableProperty] private decimal _formWholesalePrice;
    [ObservableProperty] private double _formStockQuantity;
    [ObservableProperty] private double _formMinStockLevel;
    [ObservableProperty] private string _formDescription = string.Empty;
    [ObservableProperty] private string _formUnit = string.Empty;
    [ObservableProperty] private string? _formImagePath;
    [ObservableProperty] private string _formImagePreviewPath = string.Empty;

    public ProductsViewModel(IProductService productService) { _productService = productService; }

    [RelayCommand]
    private async Task LoadProductsAsync()
    {
        IsLoading = true;
        StatusMessage = string.Empty;
        try
        {
            Products = new ObservableCollection<Product>(
                await _productService.GetProductsAsync(SearchText, SelectedCategoryId, CurrentPage, PageSize));
            TotalCount = await _productService.GetTotalCountAsync(SearchText, SelectedCategoryId);
            OnPropertyChanged(nameof(TotalPages));
        }
        catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task LoadCategoriesAsync()
    {
        try { Categories = new ObservableCollection<Category>(await _productService.GetCategoriesAsync()); }
        catch (Exception ex) { StatusMessage = $"Error loading categories: {ex.Message}"; }
    }

    [RelayCommand]
    private async Task NextPageAsync()
    {
        if (CurrentPage < TotalPages) { CurrentPage++; await LoadProductsAsync(); }
    }

    [RelayCommand]
    private async Task PreviousPageAsync()
    {
        if (CurrentPage > 1) { CurrentPage--; await LoadProductsAsync(); }
    }

    [RelayCommand]
    private async Task SearchAsync() { CurrentPage = 1; await LoadProductsAsync(); }

    [RelayCommand]
    private void ShowAddForm() { ResetForm(); ShowForm = true; IsEditing = false; FormTitle = "Add Product"; }

    [RelayCommand]
    private void ShowEditForm(Product product)
    {
        if (product == null) return;
        FormId = product.Id;
        FormName = product.Name;
        FormNameAr = product.NameAr;
        FormBarcode = product.Barcode;
        FormSku = product.SKU;
        FormCategoryId = product.CategoryId;
        FormCostPrice = product.CostPrice;
        FormRetailPrice = product.RetailPrice;
        FormWholesalePrice = product.WholesalePrice;
        FormStockQuantity = product.StockQuantity;
        FormMinStockLevel = product.MinStockLevel;
        FormDescription = product.Description ?? string.Empty;
        FormUnit = product.Unit ?? string.Empty;
        FormImagePath = product.ImagePath;
        FormImagePreviewPath = product.ImagePath ?? string.Empty;
        ShowForm = true;
        IsEditing = true;
        FormTitle = "Edit Product";
    }

    [RelayCommand]
    private void CancelForm() { ShowForm = false; ResetForm(); }

    [RelayCommand]
    private void SelectImage()
    {
        var dlg = new OpenFileDialog
        {
            Filter = "Image files (*.png;*.jpg;*.jpeg;*.gif;*.bmp)|*.png;*.jpg;*.jpeg;*.gif;*.bmp|All files (*.*)|*.*",
            Title = "Select Product Image"
        };
        if (dlg.ShowDialog() == true) { FormImagePath = dlg.FileName; FormImagePreviewPath = dlg.FileName; }
    }

    [RelayCommand]
    private async Task SaveProductAsync()
    {
        if (string.IsNullOrWhiteSpace(FormName))
        {
            MessageBox.Show("Product name is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        IsLoading = true;
        try
        {
            var targetFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "KetabaPOS", "ProductImages");
            Directory.CreateDirectory(targetFolder);

            string? savedImagePath = null;
            if (!string.IsNullOrEmpty(FormImagePath))
            {
                var ext = Path.GetExtension(FormImagePath);
                var fileName = $"{Guid.NewGuid()}{ext}";
                var dest = Path.Combine(targetFolder, fileName);
                File.Copy(FormImagePath, dest, true);
                savedImagePath = dest;
            }

            if (IsEditing)
            {
                var product = await _productService.GetByIdAsync(FormId);
                if (product == null) { StatusMessage = "Product not found."; return; }
                product.Name = FormName;
                product.NameAr = FormNameAr;
                product.Barcode = FormBarcode;
                product.SKU = FormSku;
                product.CategoryId = FormCategoryId;
                product.CostPrice = FormCostPrice;
                product.RetailPrice = FormRetailPrice;
                product.WholesalePrice = FormWholesalePrice;
                product.StockQuantity = FormStockQuantity;
                product.MinStockLevel = FormMinStockLevel;
                product.Description = FormDescription;
                product.Unit = FormUnit;
                if (savedImagePath != null) product.ImagePath = savedImagePath;
                await _productService.UpdateAsync(product);
                StatusMessage = "Product updated successfully.";
            }
            else
            {
                var product = new Product
                {
                    Name = FormName, NameAr = FormNameAr, Barcode = FormBarcode, SKU = FormSku,
                    CategoryId = FormCategoryId, CostPrice = FormCostPrice, RetailPrice = FormRetailPrice,
                    WholesalePrice = FormWholesalePrice, StockQuantity = FormStockQuantity,
                    MinStockLevel = FormMinStockLevel, Description = FormDescription, Unit = FormUnit,
                    ImagePath = savedImagePath, IsActive = true
                };
                await _productService.CreateAsync(product);
                StatusMessage = "Product created successfully.";
            }
            ShowForm = false;
            ResetForm();
            await LoadProductsAsync();
        }
        catch (Exception ex) { StatusMessage = $"Save failed: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task DeleteProductAsync(Product product)
    {
        if (product == null) return;
        var result = MessageBox.Show($"Delete product \"{product.Name}\"?", "Confirm Delete",
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes)
        {
            IsLoading = true;
            try { await _productService.DeleteAsync(product.Id); StatusMessage = "Product deleted."; await LoadProductsAsync(); }
            catch (Exception ex) { StatusMessage = $"Delete failed: {ex.Message}"; }
            finally { IsLoading = false; }
        }
    }

    private void ResetForm()
    {
        FormId = 0; FormName = string.Empty; FormNameAr = string.Empty; FormBarcode = string.Empty;
        FormSku = string.Empty; FormCategoryId = 0; FormCostPrice = 0; FormRetailPrice = 0;
        FormWholesalePrice = 0; FormStockQuantity = 0; FormMinStockLevel = 0; FormDescription = string.Empty;
        FormUnit = string.Empty; FormImagePath = null; FormImagePreviewPath = string.Empty;
    }
}

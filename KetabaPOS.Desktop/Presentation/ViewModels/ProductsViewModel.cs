using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KetabaPOS.Desktop.Core.Interfaces;
using KetabaPOS.Desktop.Core.Models;

namespace KetabaPOS.Desktop.Presentation.ViewModels;

public partial class ProductsViewModel : ObservableObject
{
    private readonly IProductService _productService;

    [ObservableProperty]
    private ObservableCollection<Product> _products = new();

    [ObservableProperty]
    private ObservableCollection<Category> _categories = new();

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private int? _selectedCategoryId;

    [ObservableProperty]
    private int _currentPage = 1;

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private int _pageSize = 20;

    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    [ObservableProperty]
    private bool _isLoading;

    public ProductsViewModel(IProductService productService)
    {
        _productService = productService;
    }

    [RelayCommand]
    private async Task LoadProductsAsync()
    {
        IsLoading = true;
        try
        {
            var products = await _productService.GetProductsAsync(SearchText, SelectedCategoryId, CurrentPage, PageSize);
            Products = new ObservableCollection<Product>(products);
            TotalCount = await _productService.GetTotalCountAsync(SearchText, SelectedCategoryId);
            OnPropertyChanged(nameof(TotalPages));
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task NextPageAsync()
    {
        if (CurrentPage < TotalPages)
        {
            CurrentPage++;
            await LoadProductsAsync();
        }
    }

    [RelayCommand]
    private async Task PreviousPageAsync()
    {
        if (CurrentPage > 1)
        {
            CurrentPage--;
            await LoadProductsAsync();
        }
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        CurrentPage = 1;
        await LoadProductsAsync();
    }
}

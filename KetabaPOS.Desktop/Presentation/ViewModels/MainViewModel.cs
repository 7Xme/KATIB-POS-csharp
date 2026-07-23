using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KetabaPOS.Desktop.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace KetabaPOS.Desktop.Presentation.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IServiceProvider _serviceProvider;

    [ObservableProperty]
    private bool _isAuthenticated;

    [ObservableProperty]
    private User? _currentUser;

    [ObservableProperty]
    private bool _isRtl;

    [ObservableProperty]
    private bool _isDarkTheme;

    [ObservableProperty]
    private Visibility _sideMenuVisibility = Visibility.Visible;

    [ObservableProperty]
    private string _currentView = "Dashboard";

    [ObservableProperty]
    private object? _currentViewModel;

    private readonly Dictionary<string, object> _viewModels = new();

    public MainViewModel(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void Initialize()
    {
        NavigateTo("Dashboard");
    }

    partial void OnCurrentViewChanged(string value)
    {
        NavigateTo(value);
    }

    public void NavigateTo(string viewName)
    {
        if (_viewModels.TryGetValue(viewName, out var cached))
        {
            CurrentViewModel = cached;
            return;
        }

        object? vm = viewName switch
        {
            "Dashboard" => _serviceProvider.GetService<DashboardViewModel>(),
            "Pos" => _serviceProvider.GetService<PosViewModel>(),
            "Products" => _serviceProvider.GetService<ProductsViewModel>(),
            "Customers" => _serviceProvider.GetService<CustomersViewModel>(),
            "Suppliers" => _serviceProvider.GetService<SuppliersViewModel>(),
            "Sales" => _serviceProvider.GetService<SalesViewModel>(),
            "Loans" => _serviceProvider.GetService<LoansViewModel>(),
            "Settings" => _serviceProvider.GetService<SettingsViewModel>(),
            _ => _serviceProvider.GetService<DashboardViewModel>()
        };

        if (vm != null)
        {
            _viewModels[viewName] = vm;
            CurrentViewModel = vm;
            CurrentView = viewName;

            if (vm is DashboardViewModel dvm) dvm.LoadDashboardCommand.Execute(null);
            if (vm is PosViewModel pvm) pvm.LoadProductsCommand.Execute(null);
            if (vm is ProductsViewModel prvm) prvm.LoadProductsCommand.Execute(null);
            if (vm is CustomersViewModel cvm) cvm.LoadCustomersCommand.Execute(null);
            if (vm is SuppliersViewModel svm) svm.LoadSuppliersCommand.Execute(null);
            if (vm is SalesViewModel savm) savm.LoadSalesCommand.Execute(null);
            if (vm is LoansViewModel lvm) lvm.LoadLoansCommand.Execute(null);
            if (vm is SettingsViewModel sevm) sevm.LoadSettingsCommand.Execute(null);
        }
    }

    [RelayCommand]
    private void Logout()
    {
        IsAuthenticated = false;
        CurrentUser = null;
    }

    [RelayCommand]
    private void ToggleTheme() => IsDarkTheme = !IsDarkTheme;

    [RelayCommand]
    private void ToggleRtl() => IsRtl = !IsRtl;

    [RelayCommand]
    private void ToggleSideMenu()
    {
        SideMenuVisibility = SideMenuVisibility == Visibility.Visible
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    [RelayCommand]
    private void Navigate(string viewName) => NavigateTo(viewName);
}

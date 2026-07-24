using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KetabaPOS.Desktop.Core.Models;
using Microsoft.Extensions.DependencyInjection;

namespace KetabaPOS.Desktop.Presentation.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IServiceProvider _serviceProvider;

    [ObservableProperty] private bool _isAuthenticated;
    [ObservableProperty] private User? _currentUser;
    [ObservableProperty] private string _currentView = "Dashboard";
    [ObservableProperty] private object? _currentViewModel;

    private readonly Dictionary<string, object> _viewModels = new();

    public MainViewModel(IServiceProvider serviceProvider) { _serviceProvider = serviceProvider; }

    public void Initialize() { NavigateTo("Dashboard"); }

    partial void OnCurrentViewChanged(string value) { NavigateTo(value); }

    public void NavigateTo(string viewName)
    {
        if (_viewModels.TryGetValue(viewName, out var cached))
        {
            CurrentViewModel = cached;
            CurrentView = viewName;
            RefreshViewModel(cached);
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
            "Users" => _serviceProvider.GetService<UsersViewModel>(),
            "Purchases" => _serviceProvider.GetService<PurchasesViewModel>(),
            "Settings" => _serviceProvider.GetService<SettingsViewModel>(),
            _ => _serviceProvider.GetService<DashboardViewModel>()
        };

        if (vm != null)
        {
            _viewModels[viewName] = vm;
            CurrentViewModel = vm;
            CurrentView = viewName;
            RefreshViewModel(vm);
        }
    }

    private void RefreshViewModel(object vm)
    {
        if (vm is DashboardViewModel dvm) dvm.LoadDashboardCommand.Execute(null);
        if (vm is PosViewModel pvm) { pvm.LoadProductsCommand.Execute(null); pvm.LoadCustomersCommand.Execute(null); }
        if (vm is ProductsViewModel prvm) { prvm.LoadCategoriesCommand.Execute(null); prvm.LoadProductsCommand.Execute(null); }
        if (vm is CustomersViewModel cvm) cvm.LoadCustomersCommand.Execute(null);
        if (vm is SuppliersViewModel svm) svm.LoadSuppliersCommand.Execute(null);
        if (vm is SalesViewModel savm) savm.LoadSalesCommand.Execute(null);
        if (vm is LoansViewModel lvm) { lvm.LoadCustomersCommand.Execute(null); lvm.LoadSuppliersCommand.Execute(null); lvm.LoadLoansCommand.Execute(null); }
        if (vm is UsersViewModel uvm) uvm.LoadUsersCommand.Execute(null);
        if (vm is PurchasesViewModel puvm) { puvm.LoadSuppliersCommand.Execute(null); puvm.LoadPurchasesCommand.Execute(null); }
        if (vm is SettingsViewModel sevm) sevm.LoadSettingsCommand.Execute(null);
    }

    [RelayCommand] private void Logout() { IsAuthenticated = false; CurrentUser = null; }
    [RelayCommand] private void Navigate(string viewName) => NavigateTo(viewName);
}

using System.Windows;
using System.Windows.Input;
using KetabaPOS.Desktop.Presentation.ViewModels;

namespace KetabaPOS.Desktop;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow(MainViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        DataContext = viewModel;
        Loaded += (s, e) => viewModel.Initialize();
        KeyDown += OnKeyDown;
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F1) { _viewModel.NavigateTo("Dashboard"); e.Handled = true; return; }
        if (e.Key == Key.F2) { _viewModel.NavigateTo("Pos"); e.Handled = true; return; }
        if (e.Key == Key.F3) { _viewModel.NavigateTo("Products"); e.Handled = true; return; }
        if (e.Key == Key.F4) { _viewModel.NavigateTo("Customers"); e.Handled = true; return; }
        if (e.Key == Key.F5) { _viewModel.NavigateTo("Suppliers"); e.Handled = true; return; }
        if (e.Key == Key.F6) { _viewModel.NavigateTo("Sales"); e.Handled = true; return; }
        if (e.Key == Key.F7) { _viewModel.NavigateTo("Purchases"); e.Handled = true; return; }
        if (e.Key == Key.F8) { _viewModel.NavigateTo("Loans"); e.Handled = true; return; }
        if (e.Key == Key.F9) { _viewModel.NavigateTo("Users"); e.Handled = true; return; }
        if (e.Key == Key.F10) { _viewModel.NavigateTo("Settings"); e.Handled = true; return; }

        if (Keyboard.Modifiers == ModifierKeys.Control)
        {
            if (e.Key == Key.N) { _viewModel.NavigateTo("Pos"); e.Handled = true; return; }
            if (e.Key == Key.P && _viewModel.CurrentViewModel is SalesViewModel sv)
                { sv.PrintReceiptCommand.Execute(sv.SelectedSale); e.Handled = true; return; }
            if (e.Key == Key.F) { _viewModel.NavigateTo("Products"); e.Handled = true; return; }
        }

        if (e.Key == Key.Escape)
        {
            if (_viewModel.CurrentViewModel is ProductsViewModel pvm && pvm.ShowForm) { pvm.CancelFormCommand.Execute(null); e.Handled = true; return; }
            if (_viewModel.CurrentViewModel is SalesViewModel svm && svm.ShowDetail) { svm.CloseDetailCommand.Execute(null); e.Handled = true; return; }
        }
    }
}

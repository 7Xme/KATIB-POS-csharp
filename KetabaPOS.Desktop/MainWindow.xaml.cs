using System.Windows;
using KetabaPOS.Desktop.Presentation.ViewModels;

namespace KetabaPOS.Desktop;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += (s, e) => viewModel.Initialize();
    }
}

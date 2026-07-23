using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KetabaPOS.Desktop.Core.Interfaces;
using KetabaPOS.Desktop.Core.Models;
using System.Windows;

namespace KetabaPOS.Desktop.Presentation.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IAuthService _authService;

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

    public MainViewModel(IAuthService authService)
    {
        _authService = authService;
    }

    [RelayCommand]
    private async Task Login(string username)
    {
        CurrentUser = await _authService.GetCurrentUserAsync();
        IsAuthenticated = CurrentUser != null;
    }

    [RelayCommand]
    private void Logout()
    {
        _authService.Logout();
        IsAuthenticated = false;
        CurrentUser = null;
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        IsDarkTheme = !IsDarkTheme;
    }

    [RelayCommand]
    private void ToggleRtl()
    {
        IsRtl = !IsRtl;
    }

    [RelayCommand]
    private void ToggleSideMenu()
    {
        SideMenuVisibility = SideMenuVisibility == Visibility.Visible
            ? Visibility.Collapsed
            : Visibility.Visible;
    }
}

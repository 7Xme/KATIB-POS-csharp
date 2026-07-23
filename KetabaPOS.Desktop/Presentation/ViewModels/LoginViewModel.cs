using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KetabaPOS.Desktop.Core.Interfaces;
namespace KetabaPOS.Desktop.Presentation.ViewModels;
public partial class LoginViewModel : ObservableObject
{
    private readonly IAuthService _authService;
    [ObservableProperty] private string _username = string.Empty;
    [ObservableProperty] private string _password = string.Empty;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _loginSuccessful;
    public LoginViewModel(IAuthService authService) { _authService = authService; }
    [RelayCommand]
    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password)) { ErrorMessage = "Please enter username and password."; return; }
        IsLoading = true; ErrorMessage = null;
        try { var user = await _authService.LoginAsync(Username, Password); if (user != null) LoginSuccessful = true; else ErrorMessage = "Invalid username or password."; }
        catch { ErrorMessage = "An error occurred. Please try again."; }
        finally { IsLoading = false; }
    }
}

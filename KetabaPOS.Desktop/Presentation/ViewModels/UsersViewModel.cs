using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KetabaPOS.Desktop.Core.Enums;
using KetabaPOS.Desktop.Core.Interfaces;
using KetabaPOS.Desktop.Core.Models;
namespace KetabaPOS.Desktop.Presentation.ViewModels;
public partial class UsersViewModel : ObservableObject
{
    private readonly IAuthService _authService;
    [ObservableProperty] private ObservableCollection<User> _users = new();
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _showForm;
    [ObservableProperty] private string _formTitle = "Add User";
    [ObservableProperty] private int _formId;
    [ObservableProperty] private string _formUsername = string.Empty;
    [ObservableProperty] private string _formDisplayName = string.Empty;
    [ObservableProperty] private string _formPassword = string.Empty;
    [ObservableProperty] private UserRole _formRole = UserRole.Cashier;
    public Array UserRoles => Enum.GetValues<UserRole>();
    public UsersViewModel(IAuthService authService) { _authService = authService; }
    [RelayCommand]
    private async Task LoadUsersAsync()
    {
        IsLoading = true; StatusMessage = string.Empty;
        try { Users = new ObservableCollection<User>(await _authService.GetAllUsersAsync()); }
        catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
        finally { IsLoading = false; }
    }
    [RelayCommand]
    private void ShowAddForm() { ResetForm(); ShowForm = true; FormTitle = "Add User"; FormPassword = string.Empty; }
    [RelayCommand]
    private void ShowEditForm(User user)
    {
        if (user == null) return;
        FormId = user.Id; FormUsername = user.Username; FormDisplayName = user.DisplayName;
        FormRole = user.Role; FormPassword = string.Empty;
        ShowForm = true; FormTitle = "Edit User";
    }
    [RelayCommand]
    private void CancelForm() { ShowForm = false; ResetForm(); }
    [RelayCommand]
    private async Task SaveUserAsync()
    {
        if (string.IsNullOrWhiteSpace(FormUsername)) { MessageBox.Show("Username is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        if (string.IsNullOrWhiteSpace(FormDisplayName)) { MessageBox.Show("Display name is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        IsLoading = true;
        try
        {
            if (FormId == 0)
            {
                if (string.IsNullOrWhiteSpace(FormPassword)) { MessageBox.Show("Password is required for new users.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
                await _authService.CreateUserAsync(new User { Username = FormUsername, DisplayName = FormDisplayName, Role = FormRole, IsActive = true }, FormPassword);
                StatusMessage = "User created.";
            }
            else
            {
                var existing = Users.FirstOrDefault(u => u.Id == FormId);
                if (existing != null)
                {
                    existing.Username = FormUsername; existing.DisplayName = FormDisplayName; existing.Role = FormRole;
                    if (!string.IsNullOrWhiteSpace(FormPassword))
                    {
                        var currentUser = await _authService.GetCurrentUserAsync();
                        if (currentUser != null) await _authService.ChangePasswordAsync(FormId, existing.PasswordHash, FormPassword);
                    }
                    await _authService.UpdateUserAsync(existing);
                    StatusMessage = "User updated.";
                }
            }
            ShowForm = false; ResetForm(); await LoadUsersAsync();
        }
        catch (Exception ex) { StatusMessage = $"Save failed: {ex.Message}"; }
        finally { IsLoading = false; }
    }
    [RelayCommand]
    private async Task ToggleActiveAsync(User user)
    {
        if (user == null) return;
        IsLoading = true;
        try { await _authService.ToggleUserActiveAsync(user.Id); StatusMessage = user.IsActive ? "User deactivated." : "User activated."; await LoadUsersAsync(); }
        catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
        finally { IsLoading = false; }
    }
    [RelayCommand]
    private async Task DeleteUserAsync(User user)
    {
        if (user == null) return;
        if (MessageBox.Show($"Delete user \"{user.Username}\"?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        IsLoading = true;
        try { user.IsDeleted = true; await _authService.UpdateUserAsync(user); StatusMessage = "User deleted."; await LoadUsersAsync(); }
        catch (Exception ex) { StatusMessage = $"Delete failed: {ex.Message}"; }
        finally { IsLoading = false; }
    }
    private void ResetForm() { FormId = 0; FormUsername = string.Empty; FormDisplayName = string.Empty; FormPassword = string.Empty; FormRole = UserRole.Cashier; }
}

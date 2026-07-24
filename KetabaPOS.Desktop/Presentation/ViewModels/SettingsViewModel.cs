using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KetabaPOS.Desktop.Core.Interfaces;
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;

namespace KetabaPOS.Desktop.Presentation.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;

    [ObservableProperty] private string _companyName = string.Empty;
    [ObservableProperty] private string _companyNameAr = string.Empty;
    [ObservableProperty] private string _companyAddress = string.Empty;
    [ObservableProperty] private string _companyPhone = string.Empty;
    [ObservableProperty] private string _taxRate = "0";
    [ObservableProperty] private string _currency = "SAR";

    [ObservableProperty] private string _receiptHeader = "KETABA POS - RECEIPT";
    [ObservableProperty] private string _receiptFooter = "Thank you for your purchase!";
    [ObservableProperty] private string _paperFormat = "Thermal 80mm";
    [ObservableProperty] private string _receiptTextColor = "#000000";
    [ObservableProperty] private string _logoPath = string.Empty;
    [ObservableProperty] private string _logoPreviewPath = string.Empty;

    [ObservableProperty] private string _language = "en";
    [ObservableProperty] private string _theme = "Light";

    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _isLoading;

    public string[] PaperFormats { get; } = { "Thermal 80mm", "A4", "A5", "A6", "Letter" };
    public string[] Languages { get; } = { "en", "ar" };
    public string[] Themes { get; } = { "Light", "Dark" };

    public SettingsViewModel(ISettingsService settingsService) { _settingsService = settingsService; }

    [RelayCommand]
    private async Task LoadSettingsAsync()
    {
        IsLoading = true;
        StatusMessage = string.Empty;
        try
        {
            CompanyName = await _settingsService.GetSettingAsync("company_name") ?? "";
            CompanyNameAr = await _settingsService.GetSettingAsync("company_name_ar") ?? "";
            CompanyAddress = await _settingsService.GetSettingAsync("company_address") ?? "";
            CompanyPhone = await _settingsService.GetSettingAsync("company_phone") ?? "";
            TaxRate = await _settingsService.GetSettingAsync("tax_rate") ?? "0";
            Currency = await _settingsService.GetSettingAsync("currency") ?? "SAR";

            ReceiptHeader = await _settingsService.GetSettingAsync("receipt_header") ?? "KETABA POS - RECEIPT";
            ReceiptFooter = await _settingsService.GetSettingAsync("receipt_footer") ?? "Thank you for your purchase!";
            PaperFormat = await _settingsService.GetSettingAsync("paper_format") ?? "Thermal 80mm";
            ReceiptTextColor = await _settingsService.GetSettingAsync("receipt_text_color") ?? "#000000";
            LogoPath = await _settingsService.GetSettingAsync("company_logo") ?? "";
            LogoPreviewPath = LogoPath;
            Language = await _settingsService.GetSettingAsync("language") ?? "en";
            Theme = await _settingsService.GetSettingAsync("theme") ?? "Light";
            ApplyThemeAndLanguage();
        }
        catch (Exception ex) { StatusMessage = $"Error loading settings: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private void SelectLogo()
    {
        var dlg = new OpenFileDialog
        {
            Filter = "Image files (*.png;*.jpg;*.jpeg;*.gif;*.bmp)|*.png;*.jpg;*.jpeg;*.gif;*.bmp|All files (*.*)|*.*",
            Title = "Select Company Logo"
        };
        if (dlg.ShowDialog() == true)
        {
            LogoPath = dlg.FileName;
            LogoPreviewPath = dlg.FileName;
        }
    }

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        IsLoading = true;
        StatusMessage = string.Empty;
        try
        {
            await _settingsService.SetSettingAsync("company_name", CompanyName, "Company");
            await _settingsService.SetSettingAsync("company_name_ar", CompanyNameAr, "Company");
            await _settingsService.SetSettingAsync("company_address", CompanyAddress, "Company");
            await _settingsService.SetSettingAsync("company_phone", CompanyPhone, "Company");
            await _settingsService.SetSettingAsync("tax_rate", TaxRate, "Tax");
            await _settingsService.SetSettingAsync("currency", Currency, "Company");

            string? savedLogo = null;
            if (!string.IsNullOrEmpty(LogoPath) && File.Exists(LogoPath))
            {
                var targetFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "KetabaPOS", "Logos");
                Directory.CreateDirectory(targetFolder);
                var ext = Path.GetExtension(LogoPath);
                var fileName = $"logo{ext}";
                var dest = Path.Combine(targetFolder, fileName);
                File.Copy(LogoPath, dest, true);
                savedLogo = dest;
            }
            if (savedLogo != null)
                await _settingsService.SetSettingAsync("company_logo", savedLogo, "Receipt");

            await _settingsService.SetSettingAsync("receipt_header", ReceiptHeader, "Receipt");
            await _settingsService.SetSettingAsync("receipt_footer", ReceiptFooter, "Receipt");
            await _settingsService.SetSettingAsync("paper_format", PaperFormat, "Receipt");
            await _settingsService.SetSettingAsync("receipt_text_color", ReceiptTextColor, "Receipt");

            await _settingsService.SetSettingAsync("language", Language, "Localization");
            await _settingsService.SetSettingAsync("theme", Theme, "Appearance");

            ApplyThemeAndLanguage();

            StatusMessage = "Settings saved successfully!";
        }
        catch (Exception ex) { StatusMessage = $"Error saving settings: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    private void ApplyThemeAndLanguage()
    {
        var isDark = Theme == "Dark";
        var paletteHelper = new PaletteHelper();
        var theme = paletteHelper.GetTheme();
        theme.SetBaseTheme(isDark ? BaseTheme.Dark : BaseTheme.Light);
        paletteHelper.SetTheme(theme);

        if (Application.Current.MainWindow is Window main)
        {
            main.FlowDirection = Language == "ar" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        }
    }

    [RelayCommand]
    private async Task BackupDatabaseAsync()
    {
        var d = new SaveFileDialog
        {
            Filter = "Database files (*.db)|*.db|All files (*.*)|*.*",
            DefaultExt = ".db",
            FileName = $"ketaba_backup_{DateTime.Now:yyyyMMdd_HHmmss}.db"
        };
        if (d.ShowDialog() != true) return;
        IsLoading = true;
        try
        {
            StatusMessage = (await _settingsService.BackupDatabaseAsync(d.FileName))
                ? "Backup created successfully!"
                : "Backup failed.";
        }
        catch (Exception ex) { StatusMessage = $"Backup error: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task RestoreDatabaseAsync()
    {
        var d = new OpenFileDialog { Filter = "Database files (*.db)|*.db|All files (*.*)|*.*", DefaultExt = ".db" };
        if (d.ShowDialog() != true) return;
        var result = MessageBox.Show(
            "Restore will overwrite all current data and restart the application. Continue?",
            "Confirm Restore", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;
        IsLoading = true;
        try
        {
            var success = await _settingsService.RestoreDatabaseAsync(d.FileName);
            if (success)
            {
                MessageBox.Show("Database restored. The application will now restart.", "Restore Complete",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                Application.Current.Shutdown();
            }
            else { StatusMessage = "Restore failed."; }
        }
        catch (Exception ex) { StatusMessage = $"Restore error: {ex.Message}"; }
        finally { IsLoading = false; }
    }
}

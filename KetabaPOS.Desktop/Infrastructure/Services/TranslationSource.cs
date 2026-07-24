using System.ComponentModel;
using System.Globalization;
using System.Resources;
namespace KetabaPOS.Desktop.Infrastructure.Services;
public class TranslationSource : INotifyPropertyChanged
{
    private static readonly TranslationSource _instance = new();
    private readonly ResourceManager _resManager;
    public static TranslationSource Instance => _instance;
    public event PropertyChangedEventHandler? PropertyChanged;
    private TranslationSource()
    {
        _resManager = new ResourceManager("KetabaPOS.Desktop.Resources.Strings", typeof(TranslationSource).Assembly);
    }
    public string? this[string key] => _resManager.GetString(key, CultureInfo.CurrentUICulture);
    public string? Get(string key) => _resManager.GetString(key, CultureInfo.CurrentUICulture);
    public void SwitchTo(string cultureCode)
    {
        var culture = (CultureInfo)CultureInfo.GetCultureInfo(cultureCode).Clone();
        if (cultureCode.StartsWith("ar"))
        {
            var nfi = (NumberFormatInfo)culture.NumberFormat.Clone();
            nfi.DigitSubstitution = DigitShapes.None;
            culture.NumberFormat = nfi;
        }
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
    }
}

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;
namespace KetabaPOS.Desktop.Presentation.Behaviors;
public class AutoFocusBehavior : Behavior<FrameworkElement>
{
    protected override void OnAttached()
    {
        base.OnAttached();
        if (AssociatedObject.IsLoaded)
            DoFocus();
        else
            AssociatedObject.Loaded += (s, e) => DoFocus();
    }
    private void DoFocus()
    {
        AssociatedObject.Dispatcher.InvokeAsync(() =>
        {
            AssociatedObject.Focus();
            if (AssociatedObject is TextBox tb) tb.Select(tb.Text.Length, 0);
            Keyboard.Focus(AssociatedObject);
        }, System.Windows.Threading.DispatcherPriority.Background);
    }
}

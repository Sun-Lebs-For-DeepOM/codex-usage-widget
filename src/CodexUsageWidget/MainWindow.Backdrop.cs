using System.Windows;

namespace CodexUsageWidget;

public partial class MainWindow
{
    public bool NativeBackdropActive => false;

    private void InitializeBackdrop()
    {
        // DWM Acrylic has a fixed-opacity substrate. A layered window lets the
        // background and text have independent, continuously adjustable alpha.
        AllowsTransparency = true;
        ((FrameworkElement)Content).Margin = new Thickness(0);
    }

    private void UpdateNativeBackdrop() { }
}

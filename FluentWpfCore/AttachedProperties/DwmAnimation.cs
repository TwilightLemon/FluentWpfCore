using System.Windows;
using System.Windows.Interop;
using static FluentWpfCore.Helpers.WindowFlagsHelper;

namespace FluentWpfCore.AttachedProperties;

/// <summary>
/// DWM 窗口动画附加属性，为窗口启用系统级最大化/最小化动画
/// </summary>
public static class DwmAnimation
{
    public static bool GetEnableDwmAnimation(DependencyObject obj)
    {
        return (bool)obj.GetValue(EnableDwmAnimationProperty);
    }

    public static void SetEnableDwmAnimation(DependencyObject obj, bool value)
    {
        obj.SetValue(EnableDwmAnimationProperty, value);
    }

    public static readonly DependencyProperty EnableDwmAnimationProperty =
        DependencyProperty.RegisterAttached(
            "EnableDwmAnimation",
            typeof(bool),
            typeof(DwmAnimation),
            new PropertyMetadata(false, OnEnableDwmAnimationChanged));

    private static void OnEnableDwmAnimationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Window window || (bool)e.NewValue == false)
            return;

        if (window.IsLoaded)
        {
            EnableDwmAnimation(window);
        }
        else
        {
            window.SourceInitialized += Window_SourceInitialized;
        }
    }

    private static void Window_SourceInitialized(object? sender, EventArgs e)
    {
        if (sender is Window window)
        {
            EnableDwmAnimation(window);
            window.SourceInitialized -= Window_SourceInitialized;
        }
    }

    public static void EnableDwmAnimation(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        EnableDwmAnimationHwnd(hwnd, window.ResizeMode);
    }

    public static void EnableDwmAnimationHwnd(nint hwnd, ResizeMode resizeMode)
    {
        nint myStyle = (nint)(WS.WS_CAPTION | WS.WS_THICKFRAME | WS.WS_MAXIMIZEBOX | WS.WS_MINIMIZEBOX);
        if (resizeMode == ResizeMode.NoResize || resizeMode == ResizeMode.CanMinimize)
        {
            myStyle = (nint)(WS.WS_CAPTION | WS.WS_MINIMIZEBOX);
        }
        SetWindowLong(hwnd, (int)GetWindowLongFields.GWL_STYLE, myStyle);
    }
}

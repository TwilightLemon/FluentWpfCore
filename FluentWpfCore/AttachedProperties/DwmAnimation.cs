using System.Windows;
using System.Windows.Interop;
using static FluentWpfCore.Helpers.WindowFlagsHelper;

namespace FluentWpfCore.AttachedProperties;

/// <summary>
/// Attached property for enabling DWM window animations (maximize/minimize) while using custom title bars.
/// DWM 窗口动画附加属性，为使用自定义标题栏的窗口启用系统级最大化/最小化动画。
/// </summary>
/// <remarks>
/// When enabled, this provides native window animations even when the default title bar is removed.
/// Note: Enabling DWM animations will ignore Window.ResizeMode property. Use WindowChrome.ResizeBorderThickness instead.
/// 启用后，即使移除了默认标题栏也可以提供原生窗口动画。
/// 注意：启用 DWM 动画将忽略 Window.ResizeMode 属性。请使用 WindowChrome.ResizeBorderThickness 代替。
/// </remarks>
public static class DwmAnimation
{
    /// <summary>
    /// Gets a value indicating whether DWM animation is enabled for the specified dependency object.
    /// 获取指定依赖对象是否启用了 DWM 动画。
    /// </summary>
    /// <param name="obj">The dependency object to query. 要查询的依赖对象。</param>
    /// <returns>true if DWM animation is enabled; otherwise, false. 如果启用了 DWM 动画则为 true；否则为 false。</returns>
    public static bool GetEnableDwmAnimation(DependencyObject obj)
    {
        return (bool)obj.GetValue(EnableDwmAnimationProperty);
    }

    /// <summary>
    /// Sets a value indicating whether to enable DWM animation for the specified dependency object.
    /// 设置指定依赖对象是否启用 DWM 动画。
    /// </summary>
    /// <param name="obj">The dependency object to set. 要设置的依赖对象。</param>
    /// <param name="value">true to enable DWM animation; otherwise, false. 为 true 则启用 DWM 动画；否则为 false。</param>
    public static void SetEnableDwmAnimation(DependencyObject obj, bool value)
    {
        obj.SetValue(EnableDwmAnimationProperty, value);
    }

    /// <summary>
    /// Identifies the EnableDwmAnimation attached property.
    /// 标识 EnableDwmAnimation 附加属性。
    /// </summary>
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

    /// <summary>
    /// Enables DWM animation for the specified window.
    /// 为指定窗口启用 DWM 动画。
    /// </summary>
    /// <param name="window">The window to enable DWM animation for. 要启用 DWM 动画的窗口。</param>
    public static void EnableDwmAnimation(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        EnableDwmAnimationHwnd(hwnd, window.ResizeMode);
    }

    /// <summary>
    /// Enables DWM animation for the specified window handle.
    /// 为指定窗口句柄启用 DWM 动画。
    /// </summary>
    /// <param name="hwnd">The window handle. 窗口句柄。</param>
    /// <param name="resizeMode">The window's resize mode. 窗口的调整大小模式。</param>
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

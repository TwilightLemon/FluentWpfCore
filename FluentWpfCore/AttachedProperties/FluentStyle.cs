using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FluentWpfCore.Helpers;
using FluentWpfCore.Interop;

namespace FluentWpfCore.AttachedProperties;

/// <summary>
/// Attached property for applying Fluent Design visual effects to ToolTip and ContextMenu.
/// Fluent 样式附加属性，为 ToolTip 和 ContextMenu 应用 Fluent Design 视觉效果。
/// </summary>
/// <remarks>
/// This property enables acrylic blur effects and modern styling for popups.
/// 此属性为弹出窗口启用亚克力模糊效果和现代样式。
/// </remarks>
public static class FluentStyle
{
    /// <summary>
    /// Gets a value indicating whether Fluent style is enabled for the specified dependency object.
    /// 获取指定依赖对象是否启用了 Fluent 样式。
    /// </summary>
    /// <param name="obj">The dependency object to query. 要查询的依赖对象。</param>
    /// <returns>true if Fluent style is enabled; otherwise, false. 如果启用了 Fluent 样式则为 true；否则为 false。</returns>
    public static bool GetUseFluentStyle(DependencyObject obj)
    {
        return (bool)obj.GetValue(UseFluentStyleProperty);
    }

    /// <summary>
    /// Sets a value indicating whether to enable Fluent style for the specified dependency object.
    /// 设置指定依赖对象是否启用 Fluent 样式。
    /// </summary>
    /// <param name="obj">The dependency object to set. 要设置的依赖对象。</param>
    /// <param name="value">true to enable Fluent style; otherwise, false. 为 true 则启用 Fluent 样式；否则为 false。</param>
    public static void SetUseFluentStyle(DependencyObject obj, bool value)
    {
        obj.SetValue(UseFluentStyleProperty, value);
    }

    /// <summary>
    /// Identifies the UseFluentStyle attached property.
    /// 标识 UseFluentStyle 附加属性。
    /// </summary>
    public static readonly DependencyProperty UseFluentStyleProperty =
        DependencyProperty.RegisterAttached(
            "UseFluentStyle",
            typeof(bool),
            typeof(FluentStyle),
            new PropertyMetadata(false, OnUseFluentStyleChanged));

    private static void OnUseFluentStyleChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue == e.OldValue)
            return;

        bool useFluentStyle = (bool)e.NewValue;

        switch (obj)
        {
            case ToolTip tip:
                if (useFluentStyle)
                    tip.Opened += Popup_Opened;
                else
                    tip.Opened -= Popup_Opened;
                break;

            case ContextMenu menu:
                if (useFluentStyle)
                    menu.Opened += Popup_Opened;
                else
                    menu.Opened -= Popup_Opened;
                break;
        }
    }

    private static void Popup_Opened(object sender, RoutedEventArgs e)
    {
        IntPtr hwnd;
        Color color;
        MaterialApis.WindowCorner corner;

        switch (sender)
        {
            case ToolTip tip/* when tip.Background is SolidColorBrush brush*/: // as for ToolTip and ContextMenu, Background is rendered by WPF so we don't need to re-render it in the Acrylic effect.
                hwnd = tip.GetNativeWindowHwnd();
                color = Colors.Transparent;// brush.Color;
                corner = MaterialApis.WindowCorner.RoundSmall;
                break;

            case ContextMenu menu/* when menu.Background is SolidColorBrush brush*/:
                hwnd = menu.GetNativeWindowHwnd();
                color = Colors.Transparent;//brush.Color;
                corner = MaterialApis.WindowCorner.Round;
                Debug.WriteLine($"ContextMenu Handle: {hwnd}");
                break;

            default:
                return;
        }

        PopupHelper.SetPopupWindowMaterial(hwnd, color, corner);
    }
}

using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FluentWpfCore.Helpers;
using FluentWpfCore.Interop;

namespace FluentWpfCore.AttachedProperties;

/// <summary>
/// Fluent 样式附加属性，为 ToolTip 和 ContextMenu 应用毛玻璃效果
/// </summary>
public static class FluentStyle
{
    /// <summary>
    /// 获取是否使用 Fluent 样式
    /// </summary>
    public static bool GetUseFluentStyle(DependencyObject obj)
    {
        return (bool)obj.GetValue(UseFluentStyleProperty);
    }

    /// <summary>
    /// 设置是否使用 Fluent 样式
    /// </summary>
    public static void SetUseFluentStyle(DependencyObject obj, bool value)
    {
        obj.SetValue(UseFluentStyleProperty, value);
    }

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

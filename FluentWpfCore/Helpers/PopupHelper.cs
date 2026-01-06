using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media;
using FluentWpfCore.Interop;

namespace FluentWpfCore.Helpers;

/// <summary>
/// Helper class for Popup windows, providing functionality to get native window handles and apply Fluent styles.
/// Popup 窗口辅助类，提供获取原生窗口句柄和应用 Fluent 样式的功能。
/// </summary>
internal static class PopupHelper
{
    private const BindingFlags PrivateInstanceFlag = BindingFlags.NonPublic | BindingFlags.Instance;

    /// <summary>
    /// Gets the native window handle for a ToolTip.
    /// 获取 ToolTip 的原生窗口句柄。
    /// </summary>
    /// <param name="tip">The ToolTip to get the handle from. 要获取句柄的 ToolTip。</param>
    /// <returns>The native window handle. 原生窗口句柄。</returns>
    public static IntPtr GetNativeWindowHwnd(this ToolTip tip) => GetPopup(tip).GetNativeWindowHwnd();

    /// <summary>
    /// Gets the native window handle for a ContextMenu.
    /// 获取 ContextMenu 的原生窗口句柄。
    /// </summary>
    /// <param name="menu">The ContextMenu to get the handle from. 要获取句柄的 ContextMenu。</param>
    /// <returns>The native window handle. 原生窗口句柄。</returns>
    public static IntPtr GetNativeWindowHwnd(this ContextMenu menu) => GetPopup(menu).GetNativeWindowHwnd();

#if NET8_0_OR_GREATER
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_parentPopup")]
    private static extern ref Popup GetPopup(ToolTip tip);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_parentPopup")]
    private static extern ref Popup GetPopup(ContextMenu menu);
#else
    /// <summary>
    /// Gets the _parentPopup field from ToolTip using reflection.
    /// 使用反射获取 ToolTip 的 _parentPopup 字段。
    /// </summary>
    private static Popup GetPopup(ToolTip tip)
    {
        var field = typeof(ToolTip).GetField("_parentPopup", PrivateInstanceFlag);
        return field?.GetValue(tip) as Popup ?? throw new InvalidOperationException("Unable to access _parentPopup field");
    }

    /// <summary>
    /// Gets the _parentPopup field from ContextMenu using reflection.
    /// 使用反射获取 ContextMenu 的 _parentPopup 字段。
    /// </summary>
    private static Popup GetPopup(ContextMenu menu)
    {
        var field = typeof(ContextMenu).GetField("_parentPopup", PrivateInstanceFlag);
        return field?.GetValue(menu) as Popup ?? throw new InvalidOperationException("Unable to access _parentPopup field");
    }
#endif

    /// <summary>
    /// Gets the native window handle for a Popup.
    /// 获取 Popup 的原生窗口句柄。
    /// </summary>
    /// <param name="popup">The Popup to get the handle from. 要获取句柄的 Popup。</param>
    /// <returns>The native window handle, or IntPtr.Zero if unable to retrieve. 原生窗口句柄，如果无法获取则返回 IntPtr.Zero。</returns>
    public static IntPtr GetNativeWindowHwnd(this Popup popup)
    {
        var field = typeof(Popup).GetField("_secHelper", PrivateInstanceFlag);
        if (field?.GetValue(popup) is { } secHelper)
        {
            if (secHelper.GetType().GetProperty("Handle", PrivateInstanceFlag) is { } prop)
            {
                if (prop.GetValue(secHelper) is IntPtr handle)
                {
                    return handle;
                }
            }
        }
        return IntPtr.Zero;
    }

    /// <summary>
    /// Applies Fluent material effects to a Popup window.
    /// 为 Popup 窗口应用 Fluent 材质效果。
    /// </summary>
    /// <param name="hwnd">The window handle. 窗口句柄。</param>
    /// <param name="compositionColor">The composition color. 合成颜色。</param>
    /// <param name="corner">The window corner style. 窗口圆角样式。</param>
    public static void SetPopupWindowMaterial(IntPtr hwnd, Color compositionColor,
        MaterialApis.WindowCorner corner = MaterialApis.WindowCorner.Round)
    {
        if (hwnd != IntPtr.Zero)
        {
            int hexColor = compositionColor.ToHexColor();
            var hwndSource = HwndSource.FromHwnd(hwnd);
            MaterialApis.SetWindowProperties(hwndSource, 1);
            MaterialApis.SetWindowComposition(hwnd, true, hexColor);
            MaterialApis.SetWindowCorner(hwnd, corner);
        }
    }
}

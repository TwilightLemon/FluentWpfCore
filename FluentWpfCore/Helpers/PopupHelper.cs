using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media;
using FluentWpfCore.Interop;

namespace FluentWpfCore.Helpers;

/// <summary>
/// Popup 窗口辅助类，提供获取原生窗口句柄和应用 Fluent 样式的功能
/// </summary>
internal static class PopupHelper
{
    private const BindingFlags PrivateInstanceFlag = BindingFlags.NonPublic | BindingFlags.Instance;

    /// <summary>
    /// 获取 ToolTip 的原生窗口句柄
    /// </summary>
    public static IntPtr GetNativeWindowHwnd(this ToolTip tip) => GetPopup(tip).GetNativeWindowHwnd();

    /// <summary>
    /// 获取 ContextMenu 的原生窗口句柄
    /// </summary>
    public static IntPtr GetNativeWindowHwnd(this ContextMenu menu) => GetPopup(menu).GetNativeWindowHwnd();

#if NET8_0_OR_GREATER
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_parentPopup")]
    private static extern ref Popup GetPopup(ToolTip tip);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_parentPopup")]
    private static extern ref Popup GetPopup(ContextMenu menu);
#else
    /// <summary>
    /// 使用反射获取 ToolTip 的 _parentPopup 字段
    /// </summary>
    private static Popup GetPopup(ToolTip tip)
    {
        var field = typeof(ToolTip).GetField("_parentPopup", PrivateInstanceFlag);
        return field?.GetValue(tip) as Popup ?? throw new InvalidOperationException("Unable to access _parentPopup field");
    }

    /// <summary>
    /// 使用反射获取 ContextMenu 的 _parentPopup 字段
    /// </summary>
    private static Popup GetPopup(ContextMenu menu)
    {
        var field = typeof(ContextMenu).GetField("_parentPopup", PrivateInstanceFlag);
        return field?.GetValue(menu) as Popup ?? throw new InvalidOperationException("Unable to access _parentPopup field");
    }
#endif

    /// <summary>
    /// 获取 Popup 的原生窗口句柄
    /// </summary>
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
    /// 为 Popup 窗口应用 Fluent 材质效果
    /// </summary>
    /// <param name="hwnd">窗口句柄</param>
    /// <param name="compositionColor">合成颜色</param>
    /// <param name="corner">窗口圆角样式</param>
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

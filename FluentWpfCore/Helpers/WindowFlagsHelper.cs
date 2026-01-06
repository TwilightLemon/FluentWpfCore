using FluentWpfCore.Interop;
using System.Windows;
using System.Windows.Interop;

namespace FluentWpfCore.Helpers;

/// <summary>
/// Helper class for manipulating window flags and styles using Win32 APIs.
/// 窗口标志和样式操作辅助类，使用 Win32 API。
/// </summary>
public static class WindowFlagsHelper
{
    /// <summary>
    /// Extended window styles.
    /// 扩展窗口样式。
    /// </summary>
    [Flags]
    public enum ExtendedWindowStyles
    {
        /// <summary>
        /// Creates a tool window style. 创建工具窗口样式。
        /// </summary>
        WS_EX_TOOLWINDOW = 0x00000080,
        
        /// <summary>
        /// Prevents the window from becoming active. 防止窗口变为活动窗口。
        /// </summary>
        WS_EX_NOACTIVATE = 0x08000000,
    }

    /// <summary>
    /// GetWindowLong field indices.
    /// GetWindowLong 字段索引。
    /// </summary>
    public enum GetWindowLongFields
    {
        /// <summary>
        /// Extended window style. 扩展窗口样式。
        /// </summary>
        GWL_EXSTYLE = -20,
        
        /// <summary>
        /// Window style. 窗口样式。
        /// </summary>
        GWL_STYLE = -16
    }

    /// <summary>
    /// Window styles constants.
    /// 窗口样式常量。
    /// </summary>
    public static class WS
    {
        public static readonly long
        WS_BORDER = 0x00800000L,
        WS_CAPTION = 0x00C00000L,
        WS_CHILD = 0x40000000L,
        WS_CHILDWINDOW = 0x40000000L,
        WS_CLIPCHILDREN = 0x02000000L,
        WS_CLIPSIBLINGS = 0x04000000L,
        WS_DISABLED = 0x08000000L,
        WS_DLGFRAME = 0x00400000L,
        WS_GROUP = 0x00020000L,
        WS_HSCROLL = 0x00100000L,
        WS_ICONIC = 0x20000000L,
        WS_MAXIMIZE = 0x01000000L,
        WS_MAXIMIZEBOX = 0x00010000L,
        WS_MINIMIZE = 0x20000000L,
        WS_MINIMIZEBOX = 0x00020000L,
        WS_OVERLAPPED = 0x00000000L,
        WS_OVERLAPPEDWINDOW = WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX,
        WS_POPUP = 0x80000000L,
        WS_POPUPWINDOW = WS_POPUP | WS_BORDER | WS_SYSMENU,
        WS_SIZEBOX = 0x00040000L,
        WS_SYSMENU = 0x00080000L,
        WS_TABSTOP = 0x00010000L,
        WS_THICKFRAME = 0x00040000L,
        WS_TILED = 0x00000000L,
        WS_TILEDWINDOW = WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX,
        WS_VISIBLE = 0x10000000L,
        WS_VSCROLL = 0x00200000L;
    }

    /// <summary>
    /// Sets a window attribute value.
    /// 设置窗口属性值。
    /// </summary>
    /// <param name="hWnd">Window handle. 窗口句柄。</param>
    /// <param name="nIndex">The index of the attribute to set. 要设置的属性索引。</param>
    /// <param name="dwNewLong">The new attribute value. 新的属性值。</param>
    /// <returns>The previous value. 之前的值。</returns>
    public static nint SetWindowLong(nint hWnd, int nIndex, nint dwNewLong)
        =>Win32Interop.SetWindowLong(hWnd, nIndex, dwNewLong);
    /// <summary>
    /// Gets a window attribute value.
    /// 获取窗口属性值。
    /// </summary>
    /// <param name="hWnd">Window handle. 窗口句柄。</param>
    /// <param name="nIndex">The index of the attribute to get. 要获取的属性索引。</param>
    /// <returns>The attribute value. 属性值。</returns>
    public static nint GetWindowLong(nint hWnd, int nIndex)
        =>Win32Interop.GetWindowLong(hWnd, nIndex);

    /// <summary>
    /// Sets a window as a tool window.
    /// 将窗口设置为工具窗口。
    /// </summary>
    /// <param name="win">The window to set. 要设置的窗口。</param>
    public static void SetToolWindow(Window win)
    {
        WindowInteropHelper wndHelper = new(win);
        int exStyle = (int)Win32Interop.GetWindowLong(wndHelper.Handle, (int)GetWindowLongFields.GWL_EXSTYLE);
        exStyle |= (int)ExtendedWindowStyles.WS_EX_TOOLWINDOW;
        SetWindowLong(wndHelper.Handle, (int)GetWindowLongFields.GWL_EXSTYLE, exStyle);
    }

    /// <summary>
    /// Sets a window to not activate when shown.
    /// 设置窗口在显示时不激活。
    /// </summary>
    /// <param name="hwnd">Window handle. 窗口句柄。</param>
    public static void SetNoActiveWindow(nint hwnd)
    {
        int exStyle = (int)Win32Interop.GetWindowLong(hwnd, (int)GetWindowLongFields.GWL_EXSTYLE);
        exStyle |= (int)ExtendedWindowStyles.WS_EX_NOACTIVATE;
        SetWindowLong(hwnd, (int)GetWindowLongFields.GWL_EXSTYLE, exStyle);
    }

    /// <summary>
    /// Determines whether a window is maximized.
    /// 确定窗口是否已最大化。
    /// </summary>
    /// <param name="intPtr">Window handle. 窗口句柄。</param>
    /// <returns>true if the window is maximized; otherwise, false. 如果窗口已最大化则为 true；否则为 false。</returns>
    public static bool IsZoomedWindow(nint intPtr)
    {
        return Win32Interop.IsZoomed(intPtr);
    }
}

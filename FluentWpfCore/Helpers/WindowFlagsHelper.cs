using FluentWpfCore.Interop;
using System.Windows;
using System.Windows.Interop;

namespace FluentWpfCore.Helpers;

/// <summary>
/// 窗口标志辅助类，提供窗口样式和扩展样式的操作方法
/// Window flags helper class providing methods to manipulate window styles and extended styles
/// </summary>
public static class WindowFlagsHelper
{
    /// <summary>
    /// 窗口扩展样式枚举
    /// Extended window styles enumeration
    /// </summary>
    [Flags]
    public enum ExtendedWindowStyles
    {
        /// <summary>工具窗口样式 / Tool window style</summary>
        WS_EX_TOOLWINDOW = 0x00000080,
        /// <summary>不激活窗口样式 / No activate window style</summary>
        WS_EX_NOACTIVATE = 0x08000000,
    }

    /// <summary>
    /// GetWindowLong 函数的字段索引
    /// Field indices for GetWindowLong function
    /// </summary>
    public enum GetWindowLongFields
    {
        /// <summary>扩展样式 / Extended style</summary>
        GWL_EXSTYLE = -20,
        /// <summary>窗口样式 / Window style</summary>
        GWL_STYLE = -16
    }

    /// <summary>
    /// 窗口样式常量
    /// Window style constants
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
    /// 设置窗口长整型值
    /// Sets a window long value
    /// </summary>
    /// <param name="hWnd">窗口句柄 / Window handle</param>
    /// <param name="nIndex">字段索引 / Field index</param>
    /// <param name="dwNewLong">新值 / New value</param>
    /// <returns>原值 / Previous value</returns>
    public static nint SetWindowLong(nint hWnd, int nIndex, nint dwNewLong)
        =>Win32Interop.SetWindowLong(hWnd, nIndex, dwNewLong);

    /// <summary>
    /// 获取窗口长整型值
    /// Gets a window long value
    /// </summary>
    /// <param name="hWnd">窗口句柄 / Window handle</param>
    /// <param name="nIndex">字段索引 / Field index</param>
    /// <returns>窗口长整型值 / Window long value</returns>
    public static nint GetWindowLong(nint hWnd, int nIndex)
        =>Win32Interop.GetWindowLong(hWnd, nIndex);

    /// <summary>
    /// 将窗口设置为工具窗口样式
    /// Sets window as a tool window
    /// </summary>
    /// <param name="win">目标窗口 / Target window</param>
    public static void SetToolWindow(Window win)
    {
        WindowInteropHelper wndHelper = new(win);
        int exStyle = (int)Win32Interop.GetWindowLong(wndHelper.Handle, (int)GetWindowLongFields.GWL_EXSTYLE);
        exStyle |= (int)ExtendedWindowStyles.WS_EX_TOOLWINDOW;
        SetWindowLong(wndHelper.Handle, (int)GetWindowLongFields.GWL_EXSTYLE, exStyle);
    }

    /// <summary>
    /// 将窗口设置为不激活样式
    /// Sets window as no-activate
    /// </summary>
    /// <param name="hwnd">窗口句柄 / Window handle</param>
    public static void SetNoActiveWindow(nint hwnd)
    {
        int exStyle = (int)Win32Interop.GetWindowLong(hwnd, (int)GetWindowLongFields.GWL_EXSTYLE);
        exStyle |= (int)ExtendedWindowStyles.WS_EX_NOACTIVATE;
        SetWindowLong(hwnd, (int)GetWindowLongFields.GWL_EXSTYLE, exStyle);
    }

    /// <summary>
    /// 判断窗口是否已最大化
    /// Determines if window is maximized
    /// </summary>
    /// <param name="intPtr">窗口句柄 / Window handle</param>
    /// <returns>是否已最大化 / Whether maximized</returns>
    public static bool IsZoomedWindow(nint intPtr)
    {
        return Win32Interop.IsZoomed(intPtr);
    }
}

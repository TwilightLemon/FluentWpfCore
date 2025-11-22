using System;
using System.Runtime.InteropServices;

namespace FluentWpfCore.Interop;

/// <summary>
/// Win32 API 互操作声明
/// </summary>
internal static partial class Win32Interop
{
    #region User32 - Window Style & Attributes

    [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
    private static extern int SetWindowLong32(nint hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
    private static extern nint SetWindowLongPtr64(nint hWnd, int nIndex, nint dwNewLong);

    internal static nint SetWindowLongPtr(nint hWnd, int nIndex, nint dwNewLong)
    {
        if (Environment.Is64BitProcess)
        {
            return SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
        }
        else
        {
            return SetWindowLong32(hWnd, nIndex, (int)dwNewLong);
        }
    }

    [LibraryImport("user32.dll", SetLastError = true)]
    internal static partial int SetWindowCompositionAttribute(nint hwnd, ref WindowCompositionAttributeData data);

    internal const int GWL_STYLE = -16;

    internal const long WS_CAPTION = 0x00C00000L;
    internal const long WS_MAXIMIZEBOX = 0x00010000L;
    internal const long WS_MINIMIZEBOX = 0x00020000L;
    internal const long WS_THICKFRAME = 0x00040000L;
    internal const long WS_OVERLAPPED = 0x00000000L;
    internal const long WS_SYSMENU = 0x00080000L;
    internal const long WS_BORDER = 0x00800000L;

    internal enum AccentState
    {
        ACCENT_DISABLED = 0,
        ACCENT_ENABLE_GRADIENT = 1,
        ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
        ACCENT_ENABLE_BLURBEHIND = 3,
        ACCENT_ENABLE_ACRYLICBLURBEHIND = 4,
        ACCENT_INVALID_STATE = 5,
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct AccentPolicy
    {
        public AccentState AccentState;
        public int AccentFlags;
        public int GradientColor;
        public int AnimationId;
    }

    internal enum WindowCompositionAttribute
    {
        WCA_ACCENT_POLICY = 19,
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct WindowCompositionAttributeData
    {
        public WindowCompositionAttribute Attribute;
        public nint Data;
        public int SizeOfData;
    }

    #endregion

    #region DWM - Desktop Window Manager

    [LibraryImport("dwmapi.dll")]
    internal static partial nint DwmExtendFrameIntoClientArea(nint hwnd, ref Margins margins);

    [LibraryImport("dwmapi.dll")]
    internal static partial int DwmSetWindowAttribute(nint hwnd, DWMWINDOWATTRIBUTE dwAttribute, ref int pvAttribute, int cbAttribute);

    [Flags]
    internal enum DWMWINDOWATTRIBUTE
    {
        DWMWA_USE_IMMERSIVE_DARK_MODE = 20,
        WINDOW_CORNER_PREFERENCE = 33,
        DWMWA_SYSTEMBACKDROP_TYPE = 38,
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Margins
    {
        public int LeftWidth;
        public int RightWidth;
        public int TopHeight;
        public int BottomHeight;
    }

    #endregion
}

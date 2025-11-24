using System.Runtime.InteropServices;

namespace FluentWpfCore.Interop;

/// <summary>
/// Win32 API 互操作声明
/// </summary>
internal static partial class Win32Interop
{
    #region User32 - Window Style & Attributes

#if NET7_0_OR_GREATER
    [LibraryImport("user32.dll")]
    internal static partial nint GetWindowLong(nint hWnd, int nIndex);

    [LibraryImport("user32.dll")]
    internal static partial nint SetActiveWindow(nint hWnd);
#else
    [DllImport("user32.dll")]
    internal static extern IntPtr GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    internal static extern IntPtr SetActiveWindow(IntPtr hWnd);
#endif

    [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
    private static extern int SetWindowLong32(nint hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
    private static extern nint SetWindowLongPtr64(nint hWnd, int nIndex, nint dwNewLong);

#if NET7_0_OR_GREATER
    [LibraryImport("kernel32.dll", EntryPoint = "SetLastError")]
    internal static partial void SetLastError(int dwErrorCode);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool IsZoomed(nint hWnd);
#else
    [DllImport("kernel32.dll", EntryPoint = "SetLastError")]
    internal static extern void SetLastError(int dwErrorCode);

    [DllImport("user32.dll")]
    internal static extern bool IsZoomed(IntPtr hWnd);
#endif

    internal static nint SetWindowLong(nint hWnd, int nIndex, nint dwNewLong)
    {
        int error = 0;
        nint result = 0;
        // Win32 SetWindowLong doesn't clear error on success
        SetLastError(0);

        if (IntPtr.Size == 4)
        {
            // use SetWindowLong
            int tempResult = SetWindowLong32(hWnd, nIndex, (int)dwNewLong);
            error = Marshal.GetLastWin32Error();
            result = tempResult;
        }
        else
        {
            // use SetWindowLongPtr
            result = SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
            error = Marshal.GetLastWin32Error();
        }

        if ((result == 0) && (error != 0))
        {
            throw new System.ComponentModel.Win32Exception(error);
        }

        return result;
    }

#if NET7_0_OR_GREATER
    [LibraryImport("user32.dll", SetLastError = true)]
    internal static partial int SetWindowCompositionAttribute(nint hwnd, ref WindowCompositionAttributeData data);
#else
    [DllImport("user32.dll", SetLastError = true)]
    internal static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);
#endif

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

#if NET7_0_OR_GREATER
    [LibraryImport("dwmapi.dll")]
    internal static partial nint DwmExtendFrameIntoClientArea(nint hwnd, ref Margins margins);

    [LibraryImport("dwmapi.dll")]
    internal static partial int DwmSetWindowAttribute(nint hwnd, DWMWINDOWATTRIBUTE dwAttribute, ref int pvAttribute, int cbAttribute);
#else
    [DllImport("dwmapi.dll")]
    internal static extern IntPtr DwmExtendFrameIntoClientArea(IntPtr hwnd, ref Margins margins);

    [DllImport("dwmapi.dll")]
    internal static extern int DwmSetWindowAttribute(IntPtr hwnd, DWMWINDOWATTRIBUTE dwAttribute, ref int pvAttribute, int cbAttribute);
#endif

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

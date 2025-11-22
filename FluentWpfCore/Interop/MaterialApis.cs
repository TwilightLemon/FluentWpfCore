using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Media;

namespace FluentWpfCore.Interop;

/// <summary>
/// Windows 材质 API 封装，用于应用毛玻璃、Mica 等效果
/// </summary>
public static class MaterialApis
{
    /// <summary>
    /// 窗口圆角样式
    /// </summary>
    public enum WindowCorner
    {
        Default = 0,
        DoNotRound = 1,
        Round = 2,
        RoundSmall = 3
    }

    /// <summary>
    /// 将 Color 转换为 Win32 十六进制颜色值
    /// </summary>
    public static int ToHexColor(this Color value)
    {
        return value.R << 0 | value.G << 8 | value.B << 16 | value.A << 24;
    }

    /// <summary>
    /// 设置窗口属性，使其支持透明和扩展边框
    /// </summary>
    public static void SetWindowProperties(HwndSource hwndSource, int margin)
    {
        hwndSource.CompositionTarget.BackgroundColor = Colors.Transparent;
        var margins = new Win32Interop.Margins()
        {
            LeftWidth = margin,
            TopHeight = margin,
            RightWidth = margin,
            BottomHeight = margin
        };

        Win32Interop.DwmExtendFrameIntoClientArea(hwndSource.Handle, ref margins);
    }

    /// <summary>
    /// 设置 DWM 窗口属性
    /// </summary>
    internal static int SetWindowAttribute(IntPtr hwnd, Win32Interop.DWMWINDOWATTRIBUTE attribute, int parameter)
        => Win32Interop.DwmSetWindowAttribute(hwnd, attribute, ref parameter, Marshal.SizeOf<int>());

    /// <summary>
    /// 设置窗口圆角样式
    /// </summary>
    public static void SetWindowCorner(IntPtr handle, WindowCorner corner)
    {
        SetWindowAttribute(handle, Win32Interop.DWMWINDOWATTRIBUTE.WINDOW_CORNER_PREFERENCE, (int)corner);
    }

    /// <summary>
    /// 设置窗口合成效果（Windows 10 使用的旧 API）
    /// </summary>
    public static void SetWindowComposition(IntPtr handle, bool enable, int? hexColor = null)
    {
        var accent = new Win32Interop.AccentPolicy();
        if (!enable)
        {
            accent.AccentState = Win32Interop.AccentState.ACCENT_DISABLED;
        }
        else
        {
            accent.AccentState = Win32Interop.AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND;
            accent.GradientColor = hexColor ?? 0x00000000;
        }

        var data = new Win32Interop.WindowCompositionAttributeData
        {
            Attribute = Win32Interop.WindowCompositionAttribute.WCA_ACCENT_POLICY,
            SizeOfData = Marshal.SizeOf<Win32Interop.AccentPolicy>(),
            Data = Marshal.AllocHGlobal(Marshal.SizeOf<Win32Interop.AccentPolicy>())
        };

        Marshal.StructureToPtr(accent, data.Data, false);
        Win32Interop.SetWindowCompositionAttribute(handle, ref data);
        Marshal.FreeHGlobal(data.Data);
    }

    /// <summary>
    /// 设置系统背景材质类型（Windows 11+）
    /// </summary>
    public static void SetBackDropType(IntPtr handle, MaterialType mode)
    {
        SetWindowAttribute(handle, Win32Interop.DWMWINDOWATTRIBUTE.DWMWA_SYSTEMBACKDROP_TYPE, (int)mode);
    }

    /// <summary>
    /// 设置暗色模式
    /// </summary>
    public static void SetDarkMode(IntPtr handle, bool isDarkMode)
    {
        SetWindowAttribute(handle, Win32Interop.DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE, isDarkMode ? 1 : 0);
    }
}

/// <summary>
/// 材质类型
/// </summary>
public enum MaterialType
{
    None = 1,
    Mica = 2,
    Acrylic = 3,
    MicaAlt = 4
}

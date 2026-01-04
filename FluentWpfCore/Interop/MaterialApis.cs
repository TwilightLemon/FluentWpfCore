using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Media;

namespace FluentWpfCore.Interop;

/// <summary>
/// Windows 材质 API 封装，用于应用毛玻璃、Mica 等效果
/// Windows material API wrapper for applying Acrylic, Mica and other effects
/// </summary>
public static class MaterialApis
{
    /// <summary>
    /// 窗口圆角样式枚举
    /// Window corner style enumeration
    /// </summary>
    public enum WindowCorner
    {
        /// <summary>默认圆角 / Default corner</summary>
        Default = 0,
        /// <summary>不使用圆角 / No rounding</summary>
        DoNotRound = 1,
        /// <summary>圆角 / Rounded corners</summary>
        Round = 2,
        /// <summary>小圆角 / Small rounded corners</summary>
        RoundSmall = 3
    }

    /// <summary>
    /// 将 Color 转换为 Win32 十六进制颜色值 (ABGR格式)
    /// Converts Color to Win32 hexadecimal color value (ABGR format)
    /// </summary>
    /// <param name="value">WPF颜色 / WPF color</param>
    /// <returns>十六进制颜色值 / Hexadecimal color value</returns>
    public static int ToHexColor(this Color value)
    {
        return value.R << 0 | value.G << 8 | value.B << 16 | value.A << 24;
    }

    /// <summary>
    /// 设置窗口属性，使其支持透明和扩展边框
    /// Sets window properties to support transparency and extended frame
    /// </summary>
    /// <param name="hwndSource">窗口源 / Window source</param>
    /// <param name="margin">边框边距 / Frame margin</param>
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
    /// Sets DWM window attribute
    /// </summary>
    /// <param name="hwnd">窗口句柄 / Window handle</param>
    /// <param name="attribute">属性类型 / Attribute type</param>
    /// <param name="parameter">属性值 / Attribute value</param>
    /// <returns>操作结果 / Operation result</returns>
    internal static int SetWindowAttribute(IntPtr hwnd, Win32Interop.DWMWINDOWATTRIBUTE attribute, int parameter)
        => Win32Interop.DwmSetWindowAttribute(hwnd, attribute, ref parameter, Marshal.SizeOf(typeof(int)));

    /// <summary>
    /// 设置窗口圆角样式
    /// Sets window corner style
    /// </summary>
    /// <param name="handle">窗口句柄 / Window handle</param>
    /// <param name="corner">圆角样式 / Corner style</param>
    public static void SetWindowCorner(IntPtr handle, WindowCorner corner)
    {
        SetWindowAttribute(handle, Win32Interop.DWMWINDOWATTRIBUTE.WINDOW_CORNER_PREFERENCE, (int)corner);
    }

    /// <summary>
    /// 设置窗口合成效果（Windows 10 使用的旧 API）
    /// Sets window composition effect (legacy API used on Windows 10)
    /// </summary>
    /// <param name="handle">窗口句柄 / Window handle</param>
    /// <param name="enable">是否启用 / Whether to enable</param>
    /// <param name="hexColor">十六进制颜色值（可选）/ Hexadecimal color value (optional)</param>
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
            SizeOfData = Marshal.SizeOf(typeof(Win32Interop.AccentPolicy)),
            Data = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Win32Interop.AccentPolicy)))
        };

        Marshal.StructureToPtr(accent, data.Data, false);
        Win32Interop.SetWindowCompositionAttribute(handle, ref data);
        Marshal.FreeHGlobal(data.Data);
    }

    /// <summary>
    /// 设置系统背景材质类型（Windows 11+）
    /// Sets system backdrop type (Windows 11+)
    /// </summary>
    /// <param name="handle">窗口句柄 / Window handle</param>
    /// <param name="mode">材质类型 / Material type</param>
    public static void SetBackDropType(IntPtr handle, MaterialType mode)
    {
        SetWindowAttribute(handle, Win32Interop.DWMWINDOWATTRIBUTE.DWMWA_SYSTEMBACKDROP_TYPE, (int)mode);
    }

    /// <summary>
    /// 设置暗色模式
    /// Sets dark mode
    /// </summary>
    /// <param name="handle">窗口句柄 / Window handle</param>
    /// <param name="isDarkMode">是否使用暗色模式 / Whether to use dark mode</param>
    public static void SetDarkMode(IntPtr handle, bool isDarkMode)
    {
        SetWindowAttribute(handle, Win32Interop.DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE, isDarkMode ? 1 : 0);
    }
}

/// <summary>
/// 材质类型枚举
/// Material type enumeration
/// </summary>
public enum MaterialType
{
    /// <summary>无材质 / No material</summary>
    None = 1,
    /// <summary>云母材质 / Mica material</summary>
    Mica = 2,
    /// <summary>亚克力材质 / Acrylic material</summary>
    Acrylic = 3,
    /// <summary>云母Alt材质 / Mica Alt material</summary>
    MicaAlt = 4
}

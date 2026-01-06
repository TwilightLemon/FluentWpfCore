using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Media;

namespace FluentWpfCore.Interop;

/// <summary>
/// Windows material API wrapper for applying Acrylic, Mica, and other visual effects.
/// Windows 材质 API 封装，用于应用亚克力、Mica 等视觉效果。
/// </summary>
public static class MaterialApis
{
    /// <summary>
    /// Window corner style preference.
    /// 窗口圆角样式。
    /// </summary>
    public enum WindowCorner
    {
        /// <summary>
        /// System default corner style. 系统默认圆角样式。
        /// </summary>
        Default = 0,
        
        /// <summary>
        /// Do not round window corners. 不使用圆角。
        /// </summary>
        DoNotRound = 1,
        
        /// <summary>
        /// Rounded corners. 圆角。
        /// </summary>
        Round = 2,
        
        /// <summary>
        /// Small rounded corners. 小圆角。
        /// </summary>
        RoundSmall = 3
    }

    /// <summary>
    /// Converts a Color to Win32 hexadecimal color value.
    /// 将 Color 转换为 Win32 十六进制颜色值。
    /// </summary>
    /// <param name="value">The color to convert. 要转换的颜色。</param>
    /// <returns>Win32 hexadecimal color value. Win32 十六进制颜色值。</returns>
    public static int ToHexColor(this Color value)
    {
        return value.R << 0 | value.G << 8 | value.B << 16 | value.A << 24;
    }

    /// <summary>
    /// Sets window properties to support transparency and extended frame.
    /// 设置窗口属性，使其支持透明和扩展边框。
    /// </summary>
    /// <param name="hwndSource">The window's HWND source. 窗口的 HWND 源。</param>
    /// <param name="margin">The margin value for extending the frame. 扩展边框的边距值。</param>
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
    /// Sets a DWM window attribute.
    /// 设置 DWM 窗口属性。
    /// </summary>
    /// <param name="hwnd">Window handle. 窗口句柄。</param>
    /// <param name="attribute">The attribute to set. 要设置的属性。</param>
    /// <param name="parameter">The parameter value. 参数值。</param>
    /// <returns>Result code. 结果代码。</returns>
    internal static int SetWindowAttribute(IntPtr hwnd, Win32Interop.DWMWINDOWATTRIBUTE attribute, int parameter)
        => Win32Interop.DwmSetWindowAttribute(hwnd, attribute, ref parameter, Marshal.SizeOf(typeof(int)));

    /// <summary>
    /// Sets the window corner style.
    /// 设置窗口圆角样式。
    /// </summary>
    /// <param name="handle">Window handle. 窗口句柄。</param>
    /// <param name="corner">The corner style to apply. 要应用的圆角样式。</param>
    public static void SetWindowCorner(IntPtr handle, WindowCorner corner)
    {
        SetWindowAttribute(handle, Win32Interop.DWMWINDOWATTRIBUTE.WINDOW_CORNER_PREFERENCE, (int)corner);
    }

    /// <summary>
    /// Sets window composition effect using the legacy API (Windows 10).
    /// 使用旧版 API 设置窗口合成效果（Windows 10）。
    /// </summary>
    /// <param name="handle">Window handle. 窗口句柄。</param>
    /// <param name="enable">Whether to enable the effect. 是否启用效果。</param>
    /// <param name="hexColor">Optional hexadecimal color value. 可选的十六进制颜色值。</param>
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
    /// Sets the system backdrop material type (Windows 11+).
    /// 设置系统背景材质类型（Windows 11+）。
    /// </summary>
    /// <param name="handle">Window handle. 窗口句柄。</param>
    /// <param name="mode">The material type to apply. 要应用的材质类型。</param>
    public static void SetBackDropType(IntPtr handle, MaterialType mode)
    {
        SetWindowAttribute(handle, Win32Interop.DWMWINDOWATTRIBUTE.DWMWA_SYSTEMBACKDROP_TYPE, (int)mode);
    }

    /// <summary>
    /// Sets the window's dark mode preference.
    /// 设置窗口的暗色模式。
    /// </summary>
    /// <param name="handle">Window handle. 窗口句柄。</param>
    /// <param name="isDarkMode">Whether to use dark mode. 是否使用暗色模式。</param>
    public static void SetDarkMode(IntPtr handle, bool isDarkMode)
    {
        SetWindowAttribute(handle, Win32Interop.DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE, isDarkMode ? 1 : 0);
    }
}

/// <summary>
/// Material type for window backdrop effects.
/// 窗口背景材质类型。
/// </summary>
public enum MaterialType
{
    /// <summary>
    /// No material effect. 无材质效果。
    /// </summary>
    None = 1,
    
    /// <summary>
    /// Mica material (Windows 11+). Mica 材质（Windows 11+）。
    /// </summary>
    Mica = 2,
    
    /// <summary>
    /// Acrylic material (blurred transparency). 亚克力材质（模糊透明效果）。
    /// </summary>
    Acrylic = 3,
    
    /// <summary>
    /// Mica Alt material (Windows 11+). Mica Alt 材质（Windows 11+）。
    /// </summary>
    MicaAlt = 4
}

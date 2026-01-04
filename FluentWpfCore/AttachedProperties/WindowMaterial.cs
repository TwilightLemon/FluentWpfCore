using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shell;
using FluentWpfCore.Interop;

namespace FluentWpfCore.AttachedProperties;

/// <summary>
/// 窗口材质附加属性，为窗口应用 Acrylic、Mica 等材质效果
/// Window material attached property for applying Acrylic, Mica and other material effects to windows
/// </summary>
public class WindowMaterial : DependencyObject
{
    /// <summary>
    /// API类型枚举，用于跟踪当前使用的材质API
    /// API type enumeration for tracking the currently used material API
    /// </summary>
    private enum APIType
    {
        /// <summary>未使用材质 / No material</summary>
        NONE,
        /// <summary>系统背景API (Windows 11+) / System backdrop API (Windows 11+)</summary>
        SYSTEMBACKDROP,
        /// <summary>窗口组合API (Windows 10+) / Window composition API (Windows 10+)</summary>
        COMPOSITION
    }

    private Window? _window;
    private IntPtr _hWnd = IntPtr.Zero;
    private APIType _currentAPI = APIType.NONE;
    private int _blurColor;

    /// <summary>
    /// 附加的窗口对象
    /// The attached window object
    /// </summary>
    private Window? AttachedWindow
    {
        get => _window;
        set
        {
            _window = value;
            if (value != null)
            {
                _hWnd = new WindowInteropHelper(_window).Handle;
                if (_hWnd == IntPtr.Zero)
                {
                    value.SourceInitialized += AttachedWindow_SourceInitialized;
                }
                else
                {
                    InitWindow();
                }
            }
        }
    }

    #region Attached Property

    /// <summary>
    /// 获取窗口材质对象
    /// Gets the window material object
    /// </summary>
    /// <param name="obj">目标窗口 / Target window</param>
    /// <returns>窗口材质对象 / Window material object</returns>
    public static WindowMaterial GetMaterial(Window obj)
    {
        return (WindowMaterial)obj.GetValue(MaterialProperty);
    }

    /// <summary>
    /// 设置窗口材质对象
    /// Sets the window material object
    /// </summary>
    /// <param name="obj">目标窗口 / Target window</param>
    /// <param name="value">窗口材质对象 / Window material object</param>
    public static void SetMaterial(Window obj, WindowMaterial value)
    {
        obj.SetValue(MaterialProperty, value);
    }

    /// <summary>
    /// 窗口材质附加属性
    /// Window material attached property
    /// </summary>
    public static readonly DependencyProperty MaterialProperty =
        DependencyProperty.RegisterAttached(
            "Material",
            typeof(WindowMaterial),
            typeof(WindowMaterial),
            new PropertyMetadata(null, OnMaterialChanged));

    private static void OnMaterialChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Window window && e.NewValue is WindowMaterial material)
        {
            material.AttachedWindow = window;
        }
    }



    /// <summary>
    /// 获取窗口圆角样式
    /// Gets the window corner style
    /// </summary>
    /// <param name="obj">目标对象 / Target object</param>
    /// <returns>窗口圆角样式 / Window corner style</returns>
    public static MaterialApis.WindowCorner GetWindowCorner(DependencyObject obj)
    {
        return (MaterialApis.WindowCorner)obj.GetValue(WindowCornerProperty);
    }

    /// <summary>
    /// 设置窗口圆角样式
    /// Sets the window corner style
    /// </summary>
    /// <param name="obj">目标对象 / Target object</param>
    /// <param name="value">窗口圆角样式 / Window corner style</param>
    public static void SetWindowCorner(DependencyObject obj, MaterialApis.WindowCorner value)
    {
        obj.SetValue(WindowCornerProperty, value);
    }

    /// <summary>
    /// 窗口圆角附加属性
    /// Window corner attached property
    /// </summary>
    public static readonly DependencyProperty WindowCornerProperty =
        DependencyProperty.RegisterAttached("WindowCorner", typeof(MaterialApis.WindowCorner), typeof(WindowMaterial),
            new PropertyMetadata(MaterialApis.WindowCorner.Default, OnWindowCornerChanged));

    private static void OnWindowCornerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Window window)
        {
            if (window.IsLoaded)
            {
                var corner = (MaterialApis.WindowCorner)e.NewValue;
                MaterialApis.SetWindowCorner(new WindowInteropHelper(window).Handle, corner);
            }
            else
            {
                window.SourceInitialized += ApplyWindowCorner_OnSourceInitialized;
            }
        }
    }

    private static void ApplyWindowCorner_OnSourceInitialized(object? sender, EventArgs e)
    {
        if (sender is Window window&& GetWindowCorner(window) is { }corner)
        {
            MaterialApis.SetWindowCorner(new WindowInteropHelper(window).Handle, corner);
            window.SourceInitialized -= ApplyWindowCorner_OnSourceInitialized;
        }
    }

    #endregion

    #region Dependency Properties

    /// <summary>
    /// 获取或设置是否使用深色模式（适用于Mica/MicaAlt）
    /// Gets or sets whether to use dark mode (applicable to Mica/MicaAlt)
    /// </summary>
    public bool IsDarkMode
    {
        get => (bool)GetValue(IsDarkModeProperty);
        set => SetValue(IsDarkModeProperty, value);
    }

    /// <summary>
    /// 深色模式依赖属性
    /// Dark mode dependency property
    /// </summary>
    public static readonly DependencyProperty IsDarkModeProperty =
        DependencyProperty.Register(
            nameof(IsDarkMode),
            typeof(bool),
            typeof(WindowMaterial),
            new PropertyMetadata(false, OnIsDarkModeChanged));

    /// <summary>
    /// 获取或设置材质类型
    /// Gets or sets the material type
    /// </summary>
    public MaterialType MaterialMode
    {
        get => (MaterialType)GetValue(MaterialModeProperty);
        set => SetValue(MaterialModeProperty, value);
    }

    /// <summary>
    /// 材质模式依赖属性
    /// Material mode dependency property
    /// </summary>
    public static readonly DependencyProperty MaterialModeProperty =
        DependencyProperty.Register(
            nameof(MaterialMode),
            typeof(MaterialType),
            typeof(WindowMaterial),
            new PropertyMetadata(MaterialType.None, OnMaterialModeChanged));

    /// <summary>
    /// 获取或设置自定义WindowChrome配置
    /// Gets or sets the custom WindowChrome configuration
    /// </summary>
    public WindowChrome WindowChromeEx
    {
        get => (WindowChrome)GetValue(WindowChromeExProperty);
        set => SetValue(WindowChromeExProperty, value);
    }

    /// <summary>
    /// WindowChrome扩展依赖属性
    /// WindowChrome extension dependency property
    /// </summary>
    public static readonly DependencyProperty WindowChromeExProperty =
        DependencyProperty.Register(
            nameof(WindowChromeEx),
            typeof(WindowChrome),
            typeof(WindowMaterial),
            new PropertyMetadata(null, OnWindowChromeExChanged));

    /// <summary>
    /// 获取或设置是否使用窗口组合API（Windows 10 1809+）
    /// Gets or sets whether to use window composition API (Windows 10 1809+)
    /// </summary>
    public bool UseWindowComposition
    {
        get => (bool)GetValue(UseWindowCompositionProperty);
        set => SetValue(UseWindowCompositionProperty, value);
    }

    /// <summary>
    /// 使用窗口组合依赖属性
    /// Use window composition dependency property
    /// </summary>
    public static readonly DependencyProperty UseWindowCompositionProperty =
        DependencyProperty.Register(
            nameof(UseWindowComposition),
            typeof(bool),
            typeof(WindowMaterial),
            new PropertyMetadata(false, OnUseWindowCompositionChanged));

    /// <summary>
    /// 获取或设置组合模式下的背景颜色（仅对Acrylic + UseWindowComposition=True有效）
    /// Gets or sets the composition background color (only effective for Acrylic + UseWindowComposition=True)
    /// </summary>
    public Color CompositonColor
    {
        get => (Color)GetValue(CompositonColorProperty);
        set => SetValue(CompositonColorProperty, value);
    }

    /// <summary>
    /// 组合颜色依赖属性
    /// Composition color dependency property
    /// </summary>
    public static readonly DependencyProperty CompositonColorProperty =
        DependencyProperty.Register(
            nameof(CompositonColor),
            typeof(Color),
            typeof(WindowMaterial),
            new PropertyMetadata(Color.FromArgb(180, 0, 0, 0), OnCompositionColorChanged));

    #endregion

    #region Event Handlers

    private static void OnIsDarkModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is WindowMaterial material)
        {
            material.SetDarkMode((bool)e.NewValue);
        }
    }

    private static void OnMaterialModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is WindowMaterial material)
        {
            material.Apply();
        }
    }

    private static void OnWindowChromeExChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is WindowMaterial material && e.NewValue is WindowChrome chrome && material._window != null)
        {
            WindowChrome.SetWindowChrome(material._window, chrome);
            material.Apply();
        }
    }

    private static void OnUseWindowCompositionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is WindowMaterial material)
        {
            material.Apply();
        }
    }

    private static void OnCompositionColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is WindowMaterial material)
        {
            material.SetCompositionColor((Color)e.NewValue);
            material.Apply();
        }
    }

    private void AttachedWindow_SourceInitialized(object? sender, EventArgs e)
    {
        InitWindow();
        _window!.SourceInitialized -= AttachedWindow_SourceInitialized;
    }

    #endregion

    #region Initialization & Application

    /// <summary>
    /// 初始化窗口，应用材质效果
    /// Initializes the window and applies material effects
    /// </summary>
    private void InitWindow()
    {
        _hWnd = new WindowInteropHelper(_window).Handle;
        if (WindowChromeEx != null)
        {
            WindowChrome.SetWindowChrome(_window, WindowChromeEx);
        }
        SetDarkMode(IsDarkMode);
        Apply();
    }

    /// <summary>
    /// 应用材质效果到窗口
    /// Applies material effects to the window
    /// </summary>
    private void Apply()
    {
        if (_window == null || _hWnd == IntPtr.Zero)
            return;

        bool enable = MaterialMode != MaterialType.None;
        if (!enable)
        {
            if (_currentAPI == APIType.COMPOSITION)
                SetWindowCompositon(false);
            else if (_currentAPI == APIType.SYSTEMBACKDROP)
                SetBackDropType(MaterialType.None);
            return;
        }

        var osVersion = Environment.OSVersion.Version;
        var windows10_1809 = new Version(10, 0, 17763);
        var windows11 = new Version(10, 0, 22621);
        bool isWindows10=(osVersion >= windows10_1809 && osVersion < windows11);

        if (UseWindowComposition || isWindows10)
        {
            SetWindowProperty(isWindows10 ? 1: 0);
            SetWindowCompositon(true);
        }
        else
        {
            if (_currentAPI == APIType.COMPOSITION)
                SetWindowCompositon(false);
            SetWindowProperty(-1);
            SetBackDropType(MaterialMode);
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// 设置组合颜色
    /// Sets the composition color
    /// </summary>
    /// <param name="value">颜色值 / Color value</param>
    private void SetCompositionColor(Color value)
    {
        _blurColor = value.ToHexColor();
    }

    /// <summary>
    /// 设置深色模式
    /// Sets the dark mode
    /// </summary>
    /// <param name="isDarkMode">是否深色模式 / Whether dark mode</param>
    private void SetDarkMode(bool isDarkMode)
    {
        if (_hWnd == IntPtr.Zero) return;
        MaterialApis.SetDarkMode(_hWnd, isDarkMode);
    }

    /// <summary>
    /// 设置系统背景类型（Windows 11+）
    /// Sets the system backdrop type (Windows 11+)
    /// </summary>
    /// <param name="blurMode">材质模式 / Material mode</param>
    private void SetBackDropType(MaterialType blurMode)
    {
        if (_hWnd == IntPtr.Zero) return;
        MaterialApis.SetBackDropType(_hWnd, blurMode);
        _currentAPI = blurMode == MaterialType.None ? APIType.NONE : APIType.SYSTEMBACKDROP;
    }

    /// <summary>
    /// 设置窗口组合效果（Windows 10 1809+）
    /// Sets the window composition effect (Windows 10 1809+)
    /// </summary>
    /// <param name="enable">是否启用 / Whether to enable</param>
    private void SetWindowCompositon(bool enable)
    {
        if (_hWnd == IntPtr.Zero) return;
        MaterialApis.SetWindowComposition(_hWnd, enable, _blurColor);
        _currentAPI = enable ? APIType.COMPOSITION : APIType.NONE;
    }

    /// <summary>
    /// 设置窗口属性（边距和透明度）
    /// Sets window properties (margins and transparency)
    /// </summary>
    /// <param name="margin">边距值 / Margin value
    /// Windows 10 1809: margin = 1 or 0 (disables window drop shadow)
    /// Windows 11 with SetBackDropType(): margin = -1
    /// Windows 11 with SetWindowComposition(): margin = 0
    /// </param>
    private void SetWindowProperty(int margin)
    {
        if (_hWnd == IntPtr.Zero) return;
        var hwndSource = (HwndSource)PresentationSource.FromVisual(_window);
        MaterialApis.SetWindowProperties(hwndSource, margin);
    }

    #endregion
}

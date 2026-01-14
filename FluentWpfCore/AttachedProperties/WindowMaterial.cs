using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shell;
using FluentWpfCore.Interop;

namespace FluentWpfCore.AttachedProperties;

/// <summary>
/// Attached property for applying window material effects such as Acrylic and Mica.
/// 窗口材质附加属性，为窗口应用 Acrylic、Mica 等材质效果。
/// </summary>
/// <remarks>
/// This class provides attached properties to enable modern visual effects on WPF windows.
/// Supports Windows 10 1809+ (Composition API) and Windows 11+ (System Backdrop API).
/// 此类提供附加属性以在 WPF 窗口上启用现代视觉效果。
/// 支持 Windows 10 1809+（Composition API）和 Windows 11+（System Backdrop API）。
/// </remarks>
public class WindowMaterial : DependencyObject
{
    private enum APIType
    {
        NONE,
        SYSTEMBACKDROP,
        COMPOSITION
    }

    private Window? _window;
    private IntPtr _hWnd = IntPtr.Zero;
    private APIType _currentAPI = APIType.NONE;
    private int _blurColor;

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
    /// Gets the WindowMaterial attached to the specified window.
    /// 获取附加到指定窗口的 WindowMaterial。
    /// </summary>
    /// <param name="obj">The window to get the material from. 要获取材质的窗口。</param>
    /// <returns>The attached WindowMaterial instance. 附加的 WindowMaterial 实例。</returns>
    public static WindowMaterial GetMaterial(Window obj)
    {
        return (WindowMaterial)obj.GetValue(MaterialProperty);
    }

    /// <summary>
    /// Sets the WindowMaterial for the specified window.
    /// 设置指定窗口的 WindowMaterial。
    /// </summary>
    /// <param name="obj">The window to set the material on. 要设置材质的窗口。</param>
    /// <param name="value">The WindowMaterial instance to attach. 要附加的 WindowMaterial 实例。</param>
    public static void SetMaterial(Window obj, WindowMaterial value)
    {
        obj.SetValue(MaterialProperty, value);
    }

    /// <summary>
    /// Identifies the Material attached property.
    /// 标识 Material 附加属性。
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
    /// Gets the window corner style for the specified dependency object.
    /// 获取指定依赖对象的窗口圆角样式。
    /// </summary>
    /// <param name="obj">The dependency object to get the corner style from. 要获取圆角样式的依赖对象。</param>
    /// <returns>The window corner style. 窗口圆角样式。</returns>
    public static MaterialApis.WindowCorner GetWindowCorner(DependencyObject obj)
    {
        return (MaterialApis.WindowCorner)obj.GetValue(WindowCornerProperty);
    }

    /// <summary>
    /// Sets the window corner style for the specified dependency object.
    /// 设置指定依赖对象的窗口圆角样式。
    /// </summary>
    /// <param name="obj">The dependency object to set the corner style on. 要设置圆角样式的依赖对象。</param>
    /// <param name="value">The window corner style to apply. 要应用的窗口圆角样式。</param>
    public static void SetWindowCorner(DependencyObject obj, MaterialApis.WindowCorner value)
    {
        obj.SetValue(WindowCornerProperty, value);
    }

    /// <summary>
    /// Identifies the WindowCorner attached property.
    /// 标识 WindowCorner 附加属性。
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
    /// Gets or sets a value indicating whether to use dark mode for the material effect.
    /// 获取或设置是否使用暗色模式。
    /// </summary>
    /// <remarks>
    /// Primarily affects Mica and MicaAlt materials. Effect is less pronounced on Acrylic.
    /// 主要影响 Mica 和 MicaAlt 材质。对 Acrylic 效果不明显。
    /// </remarks>
    public bool IsDarkMode
    {
        get => (bool)GetValue(IsDarkModeProperty);
        set => SetValue(IsDarkModeProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="IsDarkMode"/> dependency property.
    /// 标识 <see cref="IsDarkMode"/> 依赖属性。
    /// </summary>
    public static readonly DependencyProperty IsDarkModeProperty =
        DependencyProperty.Register(
            nameof(IsDarkMode),
            typeof(bool),
            typeof(WindowMaterial),
            new PropertyMetadata(false, OnIsDarkModeChanged));

    /// <summary>
    /// Gets or sets the material mode for the window.
    /// 获取或设置窗口的材质模式。
    /// </summary>
    /// <remarks>
    /// Supported values: None, Acrylic, Mica, MicaAlt.
    /// Mica and MicaAlt require Windows 11+.
    /// 支持的值：None、Acrylic、Mica、MicaAlt。
    /// Mica 和 MicaAlt 需要 Windows 11+。
    /// </remarks>
    public MaterialType MaterialMode
    {
        get => (MaterialType)GetValue(MaterialModeProperty);
        set => SetValue(MaterialModeProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="MaterialMode"/> dependency property.
    /// 标识 <see cref="MaterialMode"/> 依赖属性。
    /// </summary>
    public static readonly DependencyProperty MaterialModeProperty =
        DependencyProperty.Register(
            nameof(MaterialMode),
            typeof(MaterialType),
            typeof(WindowMaterial),
            new PropertyMetadata(MaterialType.None, OnMaterialModeChanged));

    /// <summary>
    /// Gets or sets the WindowChrome configuration for the window.
    /// 获取或设置窗口的 WindowChrome 配置。
    /// </summary>
    /// <remarks>
    /// Use this to customize the window's non-client area behavior when using custom title bars.
    /// 使用此属性自定义使用自定义标题栏时窗口的非客户区行为。
    /// </remarks>
    public WindowChrome WindowChromeEx
    {
        get => (WindowChrome)GetValue(WindowChromeExProperty);
        set => SetValue(WindowChromeExProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="WindowChromeEx"/> dependency property.
    /// 标识 <see cref="WindowChromeEx"/> 依赖属性。
    /// </summary>
    public static readonly DependencyProperty WindowChromeExProperty =
        DependencyProperty.Register(
            nameof(WindowChromeEx),
            typeof(WindowChrome),
            typeof(WindowMaterial),
            new PropertyMetadata(null, OnWindowChromeExChanged));

    /// <summary>
    /// Gets or sets a value indicating whether to use the legacy window composition API (Windows 10).
    /// 获取或设置是否使用旧版窗口组合 API（Windows 10）。
    /// </summary>
    /// <remarks>
    /// When true, uses the Composition API for Acrylic on Windows 10 1809+.
    /// This also allows the Acrylic effect to remain visible when the window loses focus.
    /// 为 true 时，在 Windows 10 1809+ 上使用 Composition API 实现 Acrylic 效果。
    /// 这还允许窗口失去焦点时保持 Acrylic 效果可见。
    /// </remarks>
    public bool UseWindowComposition
    {
        get => (bool)GetValue(UseWindowCompositionProperty);
        set => SetValue(UseWindowCompositionProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="UseWindowComposition"/> dependency property.
    /// 标识 <see cref="UseWindowComposition"/> 依赖属性。
    /// </summary>
    public static readonly DependencyProperty UseWindowCompositionProperty =
        DependencyProperty.Register(
            nameof(UseWindowComposition),
            typeof(bool),
            typeof(WindowMaterial),
            new PropertyMetadata(false, OnUseWindowCompositionChanged));

    /// <summary>
    /// Gets or sets the composition color for Acrylic effects.
    /// 获取或设置 Acrylic 效果的组合颜色。
    /// </summary>
    /// <remarks>
    /// Only applies when MaterialMode is Acrylic and UseWindowComposition is true.
    /// The color's alpha channel controls the opacity of the effect.
    /// 仅在 MaterialMode 为 Acrylic 且 UseWindowComposition 为 true 时有效。
    /// 颜色的 alpha 通道控制效果的不透明度。
    /// </remarks>
    public Color CompositonColor
    {
        get => (Color)GetValue(CompositonColorProperty);
        set => SetValue(CompositonColorProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="CompositonColor"/> dependency property.
    /// 标识 <see cref="CompositonColor"/> 依赖属性。
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
        var windows11 = new Version(10, 0, 22000);
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

    private void SetCompositionColor(Color value)
    {
        _blurColor = value.ToHexColor();
    }

    private void SetDarkMode(bool isDarkMode)
    {
        if (_hWnd == IntPtr.Zero) return;
        MaterialApis.SetDarkMode(_hWnd, isDarkMode);
    }

    private void SetBackDropType(MaterialType blurMode)
    {
        if (_hWnd == IntPtr.Zero) return;
        MaterialApis.SetBackDropType(_hWnd, blurMode);
        _currentAPI = blurMode == MaterialType.None ? APIType.NONE : APIType.SYSTEMBACKDROP;
    }

    private void SetWindowCompositon(bool enable)
    {
        if (_hWnd == IntPtr.Zero) return;
        MaterialApis.SetWindowComposition(_hWnd, enable, _blurColor);
        _currentAPI = enable ? APIType.COMPOSITION : APIType.NONE;
    }

    //on windows 10 1809, margin = 1 or 0 (this will disable the window drop shadow);
    //on windows 11, margin = -1 while using SetBackDropType() method
    //               margin = 0 while using SetWindowComposition() method
    private void SetWindowProperty(int margin)
    {
        if (_hWnd == IntPtr.Zero) return;
        var hwndSource = (HwndSource)PresentationSource.FromVisual(_window);
        MaterialApis.SetWindowProperties(hwndSource, margin);
    }

    #endregion
}

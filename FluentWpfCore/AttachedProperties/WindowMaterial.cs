using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shell;
using FluentWpfCore.Interop;

namespace FluentWpfCore.AttachedProperties;

/// <summary>
/// 窗口材质附加属性，为窗口应用 Acrylic、Mica 等材质效果
/// </summary>
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

    public static WindowMaterial GetMaterial(Window obj)
    {
        return (WindowMaterial)obj.GetValue(MaterialProperty);
    }

    public static void SetMaterial(Window obj, WindowMaterial value)
    {
        obj.SetValue(MaterialProperty, value);
    }

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



    public static MaterialApis.WindowCorner GetWindowCorner(DependencyObject obj)
    {
        return (MaterialApis.WindowCorner)obj.GetValue(WindowCornerProperty);
    }

    public static void SetWindowCorner(DependencyObject obj, MaterialApis.WindowCorner value)
    {
        obj.SetValue(WindowCornerProperty, value);
    }

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

    public bool IsDarkMode
    {
        get => (bool)GetValue(IsDarkModeProperty);
        set => SetValue(IsDarkModeProperty, value);
    }

    public static readonly DependencyProperty IsDarkModeProperty =
        DependencyProperty.Register(
            nameof(IsDarkMode),
            typeof(bool),
            typeof(WindowMaterial),
            new PropertyMetadata(false, OnIsDarkModeChanged));

    public MaterialType MaterialMode
    {
        get => (MaterialType)GetValue(MaterialModeProperty);
        set => SetValue(MaterialModeProperty, value);
    }

    public static readonly DependencyProperty MaterialModeProperty =
        DependencyProperty.Register(
            nameof(MaterialMode),
            typeof(MaterialType),
            typeof(WindowMaterial),
            new PropertyMetadata(MaterialType.None, OnMaterialModeChanged));

    public WindowChrome WindowChromeEx
    {
        get => (WindowChrome)GetValue(WindowChromeExProperty);
        set => SetValue(WindowChromeExProperty, value);
    }

    public static readonly DependencyProperty WindowChromeExProperty =
        DependencyProperty.Register(
            nameof(WindowChromeEx),
            typeof(WindowChrome),
            typeof(WindowMaterial),
            new PropertyMetadata(null, OnWindowChromeExChanged));

    public bool UseWindowComposition
    {
        get => (bool)GetValue(UseWindowCompositionProperty);
        set => SetValue(UseWindowCompositionProperty, value);
    }

    public static readonly DependencyProperty UseWindowCompositionProperty =
        DependencyProperty.Register(
            nameof(UseWindowComposition),
            typeof(bool),
            typeof(WindowMaterial),
            new PropertyMetadata(false, OnUseWindowCompositionChanged));

    public Color CompositonColor
    {
        get => (Color)GetValue(CompositonColorProperty);
        set => SetValue(CompositonColorProperty, value);
    }

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

        bool enable = MaterialMode != MaterialType.None || UseWindowComposition;
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

        if (UseWindowComposition || (osVersion >= windows10_1809 && osVersion < windows11))
        {
            SetWindowProperty(true);
            SetWindowCompositon(true);
        }
        else
        {
            if (_currentAPI == APIType.COMPOSITION)
                SetWindowCompositon(false);
            SetWindowProperty(false);
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

    private void SetWindowProperty(bool isLagcy = false)
    {
        if (_hWnd == IntPtr.Zero) return;
        var hwndSource = (HwndSource)PresentationSource.FromVisual(_window);
        int margin = isLagcy ? 0 : -1;
        MaterialApis.SetWindowProperties(hwndSource, margin);
    }

    #endregion
}

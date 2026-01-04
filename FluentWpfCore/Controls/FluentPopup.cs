using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;
using FluentWpfCore.AttachedProperties;
using FluentWpfCore.Helpers;
using FluentWpfCore.Interop;

namespace FluentWpfCore.Controls;

/// <summary>
/// Fluent 样式的弹出窗口，支持毛玻璃效果和动画
/// Fluent-style popup control with acrylic effect and animations
/// </summary>
public class FluentPopup : Popup
{
    /// <summary>
    /// 扩展的弹出动画类型枚举
    /// Extended popup animation type enumeration
    /// </summary>
    public enum ExPopupAnimation
    {
        /// <summary>无动画 / No animation</summary>
        None,
        /// <summary>滑动动画 / Slide animation</summary>
        Slide,
        /// <summary>淡入淡出动画 / Fade animation</summary>
        Fade
    }

    private DoubleAnimation? _popupAnimation;

    /// <summary>
    /// 初始化 FluentPopup 控件
    /// Initializes a new instance of FluentPopup
    /// </summary>
    public FluentPopup()
    {
        Opened += FluentPopup_Opened;
        Closed += FluentPopup_Closed;
    }

    #region Dependency Properties

    /// <summary>
    /// 获取或设置弹出窗口是否跟随主窗口移动
    /// Gets or sets whether the popup follows the main window when it moves
    /// </summary>
    public bool FollowWindowMoving
    {
        get => (bool)GetValue(FollowWindowMovingProperty);
        set => SetValue(FollowWindowMovingProperty, value);
    }

    /// <summary>
    /// 跟随窗口移动依赖属性
    /// FollowWindowMoving dependency property
    /// </summary>
    public static readonly DependencyProperty FollowWindowMovingProperty =
        DependencyProperty.Register(nameof(FollowWindowMoving), typeof(bool), typeof(FluentPopup),
            new PropertyMetadata(false, OnFollowWindowMovingChanged));

    /// <summary>
    /// 获取或设置弹出窗口的圆角样式
    /// Gets or sets the corner style of the popup window
    /// </summary>
    public MaterialApis.WindowCorner WindowCorner
    {
        get => (MaterialApis.WindowCorner)GetValue(WindowCornerProperty);
        set => SetValue(WindowCornerProperty, value);
    }

    /// <summary>
    /// 窗口圆角依赖属性
    /// WindowCorner dependency property
    /// </summary>
    public static readonly DependencyProperty WindowCornerProperty =
        DependencyProperty.Register(nameof(WindowCorner), typeof(MaterialApis.WindowCorner), typeof(FluentPopup),
            new PropertyMetadata(MaterialApis.WindowCorner.Round, OnWindowCornerChanged));

    /// <summary>
    /// 获取或设置弹出窗口背景色（仅支持纯色画刷）
    /// Gets or sets the popup background color (solid color brush only)
    /// </summary>
    public SolidColorBrush Background
    {
        get => (SolidColorBrush)GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }

    /// <summary>
    /// 背景依赖属性
    /// Background dependency property
    /// </summary>
    public static readonly DependencyProperty BackgroundProperty =
        DependencyProperty.Register(nameof(Background), typeof(SolidColorBrush), typeof(FluentPopup),
            new PropertyMetadata(Brushes.Transparent, OnBackgroundChanged));

    /// <summary>
    /// 获取或设置弹出动画类型
    /// Gets or sets the popup animation type
    /// </summary>
    public ExPopupAnimation ExtPopupAnimation
    {
        get => (ExPopupAnimation)GetValue(ExtPopupAnimationProperty);
        set => SetValue(ExtPopupAnimationProperty, value);
    }

    /// <summary>
    /// 扩展弹出动画依赖属性
    /// ExtPopupAnimation dependency property
    /// </summary>
    public static readonly DependencyProperty ExtPopupAnimationProperty =
        DependencyProperty.Register(nameof(ExtPopupAnimation), typeof(ExPopupAnimation), typeof(FluentPopup),
            new PropertyMetadata(ExPopupAnimation.None));

    /// <summary>
    /// 获取或设置滑动动画偏移量（像素）
    /// Gets or sets the slide animation offset in pixels
    /// </summary>
    public uint SlideAnimationOffset { get; set; } = 30;

    #endregion

    #region Event Handlers

    private static void OnFollowWindowMovingChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
    {
        if (o is FluentPopup popup && Window.GetWindow(popup) is { } window)
        {
            if (e.NewValue is true)
            {
                window.LocationChanged += popup.AttachedWindow_LocationChanged;
                window.SizeChanged += popup.AttachedWindow_SizeChanged;
            }
            else
            {
                window.LocationChanged -= popup.AttachedWindow_LocationChanged;
                window.SizeChanged -= popup.AttachedWindow_SizeChanged;
            }
        }
    }

    private static void OnWindowCornerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FluentPopup popup)
        {
            popup.ApplyWindowCorner();
        }
    }

    private static void OnBackgroundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FluentPopup popup)
        {
            popup.ApplyFluentHwnd();
        }
    }

    private void AttachedWindow_SizeChanged(object sender, SizeChangedEventArgs e) => FollowMove();

    private void AttachedWindow_LocationChanged(object? sender, EventArgs e) => FollowMove();

    private void FluentPopup_Opened(object? sender, EventArgs e)
    {
        _windowHandle = this.GetNativeWindowHwnd();
        ApplyFluentHwnd();
        Dispatcher.Invoke(RunPopupAnimation);
    }

    private void FluentPopup_Closed(object? sender, EventArgs e) => ResetAnimation();

    #endregion

    #region Animation

    /// <summary>
    /// 构建弹出动画
    /// Builds popup animation
    /// </summary>
    private void BuildAnimation()
    {
        if (ExtPopupAnimation is ExPopupAnimation.Slide)
        {
            double offset = GetAnimateFromBottom(this) ? SlideAnimationOffset : -SlideAnimationOffset;
            _popupAnimation = new DoubleAnimation(VerticalOffset + offset, VerticalOffset, TimeSpan.FromMilliseconds(300))
            {
                EasingFunction = new CubicEase()
            };
        }
        else if (ExtPopupAnimation is ExPopupAnimation.Fade)
        {
            _popupAnimation = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300));
        }
        else
        {
            _popupAnimation = null;
            BeginAnimation(VerticalOffsetProperty, null);
        }
    }

    /// <summary>
    /// 运行弹出动画
    /// Runs popup animation
    /// </summary>
    private void RunPopupAnimation()
    {
        BuildAnimation();
        if (_popupAnimation != null)
        {
            switch(ExtPopupAnimation)
            {
                case ExPopupAnimation.Slide:
                    BeginAnimation(VerticalOffsetProperty, _popupAnimation);
                    break;
                case ExPopupAnimation.Fade:
                    Child?.BeginAnimation(OpacityProperty, _popupAnimation);
                    break;
            }
        }
    }

    /// <summary>
    /// 重置动画状态
    /// Resets animation state
    /// </summary>
    private void ResetAnimation()
    {
        if (ExtPopupAnimation is ExPopupAnimation.Slide)
        {
            BeginAnimation(VerticalOffsetProperty, null);
        }
        else if (ExtPopupAnimation is ExPopupAnimation.Fade)
        {
            Child?.BeginAnimation(OpacityProperty, null);
        }
    }

    #endregion

    #region Fluent Style

    private IntPtr _windowHandle = IntPtr.Zero;

    /// <summary>
    /// 跟随窗口移动时更新弹出位置
    /// Updates popup position when following window movement
    /// </summary>
    private void FollowMove()
    {
        if (IsOpen)
        {
            CallUpdatePosition(this);
        }
    }

#if NET8_0_OR_GREATER
    /// <summary>
    /// 使用 UnsafeAccessor 调用 Popup 的内部 UpdatePosition 方法（.NET 8+）
    /// Uses UnsafeAccessor to call Popup's internal UpdatePosition method (.NET 8+)
    /// </summary>
    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "UpdatePosition")]
    private static extern void CallUpdatePosition(Popup popup);
#else
    /// <summary>
    /// 使用反射调用 Popup 的内部 UpdatePosition 方法（.NET 8以下）
    /// Uses reflection to call Popup's internal UpdatePosition method (below .NET 8)
    /// </summary>
    private static void CallUpdatePosition(Popup popup)
    {
        var method = typeof(Popup).GetMethod("UpdatePosition", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method?.Invoke(popup, null);
    }
#endif

#if NET8_0_OR_GREATER
    /// <summary>
    /// 使用 UnsafeAccessor 获取 Popup 的 AnimateFromBottom 属性（.NET 8+）
    /// Uses UnsafeAccessor to get Popup's AnimateFromBottom property (.NET 8+)
    /// </summary>
    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_AnimateFromBottom")]
    private static extern bool GetAnimateFromBottom(Popup popup);
#else
    /// <summary>
    /// 使用反射获取 Popup 的 AnimateFromBottom 属性（.NET 8以下）
    /// Uses reflection to get Popup's AnimateFromBottom property (below .NET 8)
    /// </summary>
    private static bool GetAnimateFromBottom(Popup popup)
    {
        var i = typeof(Popup).GetProperty("AnimateFromBottom", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return i?.GetValue(popup) is true;
    }
#endif

    /// <summary>
    /// 应用 Fluent 样式到弹出窗口的原生句柄
    /// Applies Fluent style to the native window handle of the popup
    /// </summary>
    private void ApplyFluentHwnd()
    {
        if (IsOpen)
        {
            PopupHelper.SetPopupWindowMaterial(_windowHandle, Background.Color, WindowCorner);
        }
    }

    /// <summary>
    /// 应用窗口圆角样式
    /// Applies window corner style
    /// </summary>
    private void ApplyWindowCorner()
    {
        if (IsOpen)
            MaterialApis.SetWindowCorner(_windowHandle, WindowCorner);
    }

#endregion
}

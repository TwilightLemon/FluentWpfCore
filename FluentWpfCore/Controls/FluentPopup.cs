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
/// A Fluent Design styled popup control with acrylic background, rounded corners, shadow, and custom animations.
/// Fluent 样式的弹出窗口控件，支持亚克力背景、圆角、阴影和自定义动画。
/// </summary>
/// <remarks>
/// This control extends the standard Popup with modern visual effects and additional features
/// such as window-following behavior and customizable entrance/exit animations.
/// 此控件扩展了标准 Popup，提供现代视觉效果和附加功能，
/// 如跟随窗口移动和可自定义的进入/退出动画。
/// </remarks>
public class FluentPopup : Popup
{
    /// <summary>
    /// Popup animation types.
    /// 弹出窗口动画类型。
    /// </summary>
    public enum ExPopupAnimation
    {
        /// <summary>
        /// No animation. 无动画。
        /// </summary>
        None,
        
        /// <summary>
        /// Slide animation. 滑动动画。
        /// </summary>
        Slide,
        
        /// <summary>
        /// Fade animation. 淡入淡出动画。
        /// </summary>
        Fade
    }

    private DoubleAnimation? _popupAnimation;

    public FluentPopup()
    {
        Opened += FluentPopup_Opened;
        Closed += FluentPopup_Closed;
    }

    #region Dependency Properties

    /// <summary>
    /// Gets or sets a value indicating whether the popup follows the parent window when it moves.
    /// 获取或设置一个值，指示弹出窗口是否跟随父窗口移动。
    /// </summary>
    public bool FollowWindowMoving
    {
        get => (bool)GetValue(FollowWindowMovingProperty);
        set => SetValue(FollowWindowMovingProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="FollowWindowMoving"/> dependency property.
    /// 标识 <see cref="FollowWindowMoving"/> 依赖属性。
    /// </summary>
    public static readonly DependencyProperty FollowWindowMovingProperty =
        DependencyProperty.Register(nameof(FollowWindowMoving), typeof(bool), typeof(FluentPopup),
            new PropertyMetadata(false, OnFollowWindowMovingChanged));

    /// <summary>
    /// Gets or sets the corner style for the popup window.
    /// 获取或设置弹出窗口的圆角样式。
    /// </summary>
    public MaterialApis.WindowCorner WindowCorner
    {
        get => (MaterialApis.WindowCorner)GetValue(WindowCornerProperty);
        set => SetValue(WindowCornerProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="WindowCorner"/> dependency property.
    /// 标识 <see cref="WindowCorner"/> 依赖属性。
    /// </summary>
    public static readonly DependencyProperty WindowCornerProperty =
        DependencyProperty.Register(nameof(WindowCorner), typeof(MaterialApis.WindowCorner), typeof(FluentPopup),
            new PropertyMetadata(MaterialApis.WindowCorner.Round, OnWindowCornerChanged));

    /// <summary>
    /// Gets or sets the background color of the popup. Only solid colors are supported.
    /// 获取或设置弹出窗口的背景颜色。仅支持纯色。
    /// </summary>
    /// <remarks>
    /// For non-solid backgrounds, keep this transparent and provide custom visuals in the popup content.
    /// 对于非纯色背景，请将其保持透明并在弹出窗口内容中提供自定义视觉效果。
    /// </remarks>
    public SolidColorBrush Background
    {
        get => (SolidColorBrush)GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="Background"/> dependency property.
    /// 标识 <see cref="Background"/> 依赖属性。
    /// </summary>
    public static readonly DependencyProperty BackgroundProperty =
        DependencyProperty.Register(nameof(Background), typeof(SolidColorBrush), typeof(FluentPopup),
            new PropertyMetadata(Brushes.Transparent, OnBackgroundChanged));

    /// <summary>
    /// Gets or sets the animation type for the popup.
    /// 获取或设置弹出窗口的动画类型。
    /// </summary>
    public ExPopupAnimation ExtPopupAnimation
    {
        get => (ExPopupAnimation)GetValue(ExtPopupAnimationProperty);
        set => SetValue(ExtPopupAnimationProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="ExtPopupAnimation"/> dependency property.
    /// 标识 <see cref="ExtPopupAnimation"/> 依赖属性。
    /// </summary>
    public static readonly DependencyProperty ExtPopupAnimationProperty =
        DependencyProperty.Register(nameof(ExtPopupAnimation), typeof(ExPopupAnimation), typeof(FluentPopup),
            new PropertyMetadata(ExPopupAnimation.None));

    /// <summary>
    /// Gets or sets the slide animation offset in pixels.
    /// 获取或设置滑动动画的偏移量（像素）。
    /// </summary>
    /// <remarks>
    /// This property controls how far the popup slides when using the Slide animation.
    /// 此属性控制使用滑动动画时弹出窗口滑动的距离。
    /// </remarks>
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

    private void FollowMove()
    {
        if (IsOpen)
        {
            CallUpdatePosition(this);
        }
    }

#if NET8_0_OR_GREATER
    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "UpdatePosition")]
    private static extern void CallUpdatePosition(Popup popup);
#else
    private static void CallUpdatePosition(Popup popup)
    {
        var method = typeof(Popup).GetMethod("UpdatePosition", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method?.Invoke(popup, null);
    }
#endif

#if NET8_0_OR_GREATER
    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_AnimateFromBottom")]
    private static extern bool GetAnimateFromBottom(Popup popup);
#else
    private static bool GetAnimateFromBottom(Popup popup)
    {
        var i = typeof(Popup).GetProperty("AnimateFromBottom", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return i?.GetValue(popup) is true;
    }
#endif

    private void ApplyFluentHwnd()
    {
        if (IsOpen)
        {
            PopupHelper.SetPopupWindowMaterial(_windowHandle, Background.Color, WindowCorner);
        }
    }

    private void ApplyWindowCorner()
    {
        if (IsOpen)
            MaterialApis.SetWindowCorner(_windowHandle, WindowCorner);
    }

#endregion
}

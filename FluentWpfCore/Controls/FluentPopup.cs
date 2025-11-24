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
/// </summary>
public class FluentPopup : Popup
{
    /// <summary>
    /// 扩展的弹出动画类型
    /// </summary>
    public enum ExPopupAnimation
    {
        None,
        Slide,
        Fade
    }

    private DoubleAnimation? _popupAnimation;

    public FluentPopup()
    {
        Opened += FluentPopup_Opened;
        Closed += FluentPopup_Closed;
    }

    #region Dependency Properties

    public bool FollowWindowMoving
    {
        get => (bool)GetValue(FollowWindowMovingProperty);
        set => SetValue(FollowWindowMovingProperty, value);
    }

    public static readonly DependencyProperty FollowWindowMovingProperty =
        DependencyProperty.Register(nameof(FollowWindowMoving), typeof(bool), typeof(FluentPopup),
            new PropertyMetadata(false, OnFollowWindowMovingChanged));

    public MaterialApis.WindowCorner WindowCorner
    {
        get => (MaterialApis.WindowCorner)GetValue(WindowCornerProperty);
        set => SetValue(WindowCornerProperty, value);
    }

    public static readonly DependencyProperty WindowCornerProperty =
        DependencyProperty.Register(nameof(WindowCorner), typeof(MaterialApis.WindowCorner), typeof(FluentPopup),
            new PropertyMetadata(MaterialApis.WindowCorner.Round, OnWindowCornerChanged));

    public SolidColorBrush Background
    {
        get => (SolidColorBrush)GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }

    public static readonly DependencyProperty BackgroundProperty =
        DependencyProperty.Register(nameof(Background), typeof(SolidColorBrush), typeof(FluentPopup),
            new PropertyMetadata(Brushes.Transparent, OnBackgroundChanged));

    public ExPopupAnimation ExtPopupAnimation
    {
        get => (ExPopupAnimation)GetValue(ExtPopupAnimationProperty);
        set => SetValue(ExtPopupAnimationProperty, value);
    }

    public static readonly DependencyProperty ExtPopupAnimationProperty =
        DependencyProperty.Register(nameof(ExtPopupAnimation), typeof(ExPopupAnimation), typeof(FluentPopup),
            new PropertyMetadata(ExPopupAnimation.None));

    /// <summary>
    /// 滑动动画偏移量
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

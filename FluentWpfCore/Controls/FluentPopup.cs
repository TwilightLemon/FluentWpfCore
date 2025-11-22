using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;
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
        SlideUp,
        SlideDown
    }

    private DoubleAnimation? _slideAni;

    static FluentPopup()
    {
        IsOpenProperty.OverrideMetadata(typeof(FluentPopup), new FrameworkPropertyMetadata(false, OnIsOpenChanged));
    }

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
    /// 滑动动画偏移量（像素）
    /// </summary>
    public uint SlideAnimationOffset { get; set; } = 50;

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

    private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FluentPopup popup && (bool)e.NewValue)
        {
            popup.BuildAnimation();
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
        if (ExtPopupAnimation is ExPopupAnimation.SlideUp or ExPopupAnimation.SlideDown)
        {
            double offset = ExtPopupAnimation == ExPopupAnimation.SlideUp ? SlideAnimationOffset : -SlideAnimationOffset;
            _slideAni = new DoubleAnimation(VerticalOffset + offset, VerticalOffset, TimeSpan.FromMilliseconds(300))
            {
                EasingFunction = new CubicEase()
            };
        }
    }

    private void RunPopupAnimation()
    {
        if (_slideAni != null)
        {
            BeginAnimation(VerticalOffsetProperty, _slideAni);
        }
    }

    private void ResetAnimation()
    {
        if (ExtPopupAnimation is ExPopupAnimation.SlideUp or ExPopupAnimation.SlideDown)
        {
            BeginAnimation(VerticalOffsetProperty, null);
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

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "UpdatePosition")]
    private static extern void CallUpdatePosition(Popup popup);

    private void ApplyFluentHwnd()
    {
        PopupHelper.SetPopupWindowMaterial(_windowHandle, Background.Color, WindowCorner);
    }

    private void ApplyWindowCorner()
    {
        MaterialApis.SetWindowCorner(_windowHandle, WindowCorner);
    }

    #endregion
}

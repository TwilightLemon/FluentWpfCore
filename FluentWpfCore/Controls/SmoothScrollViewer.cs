using System;
using FluentWpfCore.Helpers;
using FluentWpfCore.ScrollPhysics;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace FluentWpfCore.Controls;

/// <summary>
/// A ScrollViewer with smooth, fluid scrolling animations supporting mouse wheel, touchpad, and touch input.
/// 平滑滚动 ScrollViewer，支持鼠标滚轮、触控板和触摸输入的流畅滚动动画。
/// </summary>
/// <remarks>
/// This control provides enhanced scrolling behavior with customizable physics models,
/// support for both horizontal and vertical scrolling, and smooth animations.
/// 此控件提供增强的滚动行为，具有可自定义的物理模型、
/// 支持横向和纵向滚动以及平滑动画。
/// </remarks>
public class SmoothScrollViewer : ScrollViewer
{
    //TODO: Add dependency properties for this threshold.
    // may affect Visualization. One of the solutions is to set VirtualizingPanel.CacheLength.
    private const double LogicalOffsetUpdateDistanceThreshold = 20.0; // Update logical offset after accumulated movement
    private const int WM_MOUSEHWHEEL = 0x020E; // Horizontal mouse wheel message

    private const double VisualUpdateStepThreshold = 0.1d; // Minimum visual offset change to update transform

    private double _logicalOffsetVertical;   // The actual ScrollViewer vertical offset
    private double _currentVisualOffsetVertical; // The target visual vertical offset (smooth)
    private double _visualDeltaVertical;     // Visual vertical offset delta from logical offset

    private double _logicalOffsetHorizontal;   // The actual ScrollViewer horizontal offset
    private double _currentVisualOffsetHorizontal; // The target visual horizontal offset (smooth)
    private double _visualDeltaHorizontal;     // Visual horizontal offset delta from logical offset

    private long _lastTimestamp;
    private bool _isRendering;

    private double _logicalOffsetUpdateAccumulatorVertical;
    private double _logicalOffsetUpdateAccumulatorHorizontal;

    private double _lastRenderedOffsetVertical;
    private double _lastRenderedOffsetHorizontal;

    private double _lastLogicalSyncVertical;
    private double _lastLogicalSyncHorizontal;

    private TranslateTransform? _transform;
    private UIElement? _content;
    private ScrollBar? _PART_VerticalScrollBar, _PART_HorizontalScrollBar;
    private HwndSource? _hwndSource;

    private IScrollPhysics _verticalScrollPhysics = new DefaultScrollPhysics();
    private IScrollPhysics _horizontalScrollPhysics = new DefaultScrollPhysics();

    /// <summary>
    /// Gets or sets the scroll physics model that controls the scrolling animation behavior.
    /// 获取或设置控制滚动动画行为的滚动物理模型。
    /// </summary>
    /// <remarks>
    /// The physics model determines how the scrolling decelerates and stops.
    /// 物理模型决定滚动如何减速和停止。
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when value is null. 当值为 null 时引发。</exception>
    public IScrollPhysics Physics
    {
        get => _verticalScrollPhysics;
        set
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            _verticalScrollPhysics = value.Clone();
            _horizontalScrollPhysics = value.Clone();
        }
    }

    public SmoothScrollViewer()
    {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;

        CacheMode = new BitmapCache() { SnapsToDevicePixels = true};
    }

    #region Initialization

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _PART_VerticalScrollBar=base.GetTemplateChild("PART_VerticalScrollBar") as ScrollBar;
        _PART_HorizontalScrollBar=base.GetTemplateChild("PART_HorizontalScrollBar") as ScrollBar;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (Content is UIElement element)
        {
            _content = element;
            _transform = new TranslateTransform();
            element.RenderTransform = _transform;
            element.RenderTransformOrigin = new Point(0, 0);
        }
        else
        {
            throw new InvalidOperationException("SmoothScrollViewer.Content must be a UIElement.");
        }

        // Remove any existing hook before reassigning _hwndSource to avoid multiple registrations
        _hwndSource?.RemoveHook(WndProc);

        // Hook into the window's message loop for horizontal mouse wheel (touchpad horizontal scroll)
        var window = Window.GetWindow(this);
        if (window != null)
        {
            _hwndSource = PresentationSource.FromVisual(window) as HwndSource;
            _hwndSource?.AddHook(WndProc);
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        StopRendering();
        
        // Remove the hook when unloaded
        _hwndSource?.RemoveHook(WndProc);
        _hwndSource = null;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_MOUSEHWHEEL)
        {
            // Check if this control should handle the message
            if (IsVisible && IsEnabled && IsEnableSmoothScrolling && CanScrollHorizontal)
            {
                // Check if mouse is over this control using hit testing
                var mousePos = Mouse.GetPosition(this);
                if (mousePos.X >= 0 && mousePos.X <= ActualWidth && 
                    mousePos.Y >= 0 && mousePos.Y <= ActualHeight)
                {
                    // Verify the control is actually under the mouse (not obscured by other elements)
                    if (InputHitTest(mousePos) is DependencyObject{ } hitElement)
                    {
                        // Check if there's a nested SmoothScrollViewer that should handle this instead
                        var nestedScrollViewer = FindParentSmoothScrollViewer(hitElement);
                        if (nestedScrollViewer == this)
                        {
                            // Extract the delta from wParam (high word)
                            int delta = (short)((wParam.ToInt64() >> 16) & 0xFFFF);
                            bool isPreciseMode = delta % Mouse.MouseWheelDeltaForOneLine != 0;
                            HandleScroll(0, -delta,isPreciseMode);
                            handled = true;
                        }
                    }
                }
            }
        }
        return IntPtr.Zero;
    }


    private void HandleScroll(double deltaVertical, double deltaHorizontal, bool isPreciseMode=false)
    {
        if (deltaVertical == 0 && deltaHorizontal == 0) return;

        // Lock logical offset at scroll start
        if (!_isRendering)
        {
            _logicalOffsetVertical = VerticalOffset;
            _currentVisualOffsetVertical = _logicalOffsetVertical;
            _visualDeltaVertical = 0;

            _logicalOffsetHorizontal = HorizontalOffset;
            _currentVisualOffsetHorizontal = _logicalOffsetHorizontal;
            _visualDeltaHorizontal = 0;
        }

        _verticalScrollPhysics.IsPreciseMode = isPreciseMode;
        _horizontalScrollPhysics.IsPreciseMode = isPreciseMode;

        if (deltaVertical != 0)
        {
            _verticalScrollPhysics.OnScroll(deltaVertical);
        }

        if (deltaHorizontal != 0)
        {
            _horizontalScrollPhysics.OnScroll(deltaHorizontal);
        }

        StartRendering();
    }

    #endregion

    #region Input

    /// <inheritdoc/>
    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        if (!IsEnableSmoothScrolling)
        {
            base.OnMouseWheel(e);
            return;
        }

        e.Handled = true;

        // Determine scroll orientation
        bool isShiftPressed = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
        Orientation effectiveOrientation = PreferredScrollOrientation;
        
        if (AllowTogglePreferredScrollOrientationByShiftKey && isShiftPressed)
        {
            effectiveOrientation = effectiveOrientation == Orientation.Vertical ? Orientation.Horizontal : Orientation.Vertical;
        }

        bool isPreciseMode= e.Delta % Mouse.MouseWheelDeltaForOneLine != 0;

        if (effectiveOrientation == Orientation.Vertical && CanScrollVertical)
        {
            HandleScroll(e.Delta, 0, isPreciseMode);
        }
        else if (effectiveOrientation == Orientation.Horizontal && CanScrollHorizontal)
        {
            HandleScroll(0, e.Delta, isPreciseMode);
        }
    }

    protected override void OnManipulationStarting(ManipulationStartingEventArgs e)
    {
        base.OnManipulationStarting(e);

        if (!IsEnableSmoothManipulating)
        {
            return;
        }

        e.Mode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;
        e.ManipulationContainer = this;
        e.Handled = true;
    }

    protected override void OnManipulationDelta(ManipulationDeltaEventArgs e)
    {
        if (!IsEnableSmoothManipulating)
        {
            base.OnManipulationDelta(e);
            return;
        }

        Vector translation = e.DeltaManipulation.Translation;
        double deltaH = CanScrollHorizontal ? translation.X : 0;
        double deltaV = CanScrollVertical ? translation.Y : 0;

        if ((deltaH == 0 && deltaV == 0) 
            || e.DeltaManipulation.Expansion.Length != 0    // multi-point touch is not handled.
            || e.DeltaManipulation.Rotation != 0)
        {
            base.OnManipulationDelta(e);
            return;
        }

        HandleScroll(deltaV, deltaH,true);
        e.Handled = true;
    }

    protected override void OnManipulationInertiaStarting(ManipulationInertiaStartingEventArgs e)
    {
        base.OnManipulationInertiaStarting(e);

        if (!IsEnableSmoothManipulating)
        {
            return;
        }

        if (e.TranslationBehavior != null)
        {
            double speed = e.InitialVelocities.LinearVelocity.Length; // DIP per ms
            double decel = MathExtension.Clamp(speed / 800.0, 0.0012, 0.012);
            e.TranslationBehavior.DesiredDeceleration = decel;
        }

        e.Handled = true;
    }

    /// <inheritdoc/>
    protected override void OnScrollChanged(ScrollChangedEventArgs e)
    {
        base.OnScrollChanged(e);

        bool hasVerticalChange = e.VerticalChange != 0;
        bool hasHorizontalChange = e.HorizontalChange != 0;

        if (!hasVerticalChange && !hasHorizontalChange) return;

        if (hasVerticalChange)
        {
            _logicalOffsetVertical = e.VerticalOffset;

            if (_isRendering)
            {
                _visualDeltaVertical = _currentVisualOffsetVertical - _logicalOffsetVertical;
                _transform!.Y = -_visualDeltaVertical;
            }
            else
            {
                _visualDeltaVertical = 0;
                _transform!.Y = 0;
            }
        }

        if (hasHorizontalChange)
        {
            _logicalOffsetHorizontal = e.HorizontalOffset;

            if (_isRendering)
            {
                _visualDeltaHorizontal = _currentVisualOffsetHorizontal - _logicalOffsetHorizontal;
                _transform!.X = -_visualDeltaHorizontal;
            }
            else
            {
                _visualDeltaHorizontal = 0;
                _transform!.X = 0;
            }
        }
    }

    #endregion

    #region Rendering Loop

    private void StartRendering()
    {
        if (_isRendering) return;

        _lastTimestamp = Stopwatch.GetTimestamp();
        _logicalOffsetUpdateAccumulatorVertical = 0;
        _logicalOffsetUpdateAccumulatorHorizontal = 0;
        _lastLogicalSyncVertical = _currentVisualOffsetVertical;
        _lastLogicalSyncHorizontal = _currentVisualOffsetHorizontal;
        CompositionTarget.Rendering += OnRendering;
        _isRendering = true;
        _content!.IsHitTestVisible = false;
    }

    private static readonly Binding HorizontalOffsetBinding = new("HorizontalOffset") { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent), Mode = BindingMode.OneWay };
    private static readonly Binding VerticalOffsetBinding = new("VerticalOffset") { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent), Mode = BindingMode.OneWay };
    private void StopRendering()
    {
        if (!_isRendering) return;

        CompositionTarget.Rendering -= OnRendering;
        _isRendering = false;

        double finalOffsetVertical = MathExtension.Clamp(_currentVisualOffsetVertical, 0, ScrollableHeight);
        double finalOffsetHorizontal = MathExtension.Clamp(_currentVisualOffsetHorizontal, 0, ScrollableWidth);

        ScrollToVerticalOffset(finalOffsetVertical);
        ScrollToHorizontalOffset(finalOffsetHorizontal);

        _PART_VerticalScrollBar?.SetBinding(ScrollBar.ValueProperty,VerticalOffsetBinding);
        _PART_HorizontalScrollBar?.SetBinding(ScrollBar.ValueProperty,HorizontalOffsetBinding);

        _visualDeltaVertical = 0;
        _logicalOffsetVertical = finalOffsetVertical;
        _transform!.Y = 0;

        _visualDeltaHorizontal = 0;
        _logicalOffsetHorizontal = finalOffsetHorizontal;
        _transform!.X = 0;
        
        _content!.IsHitTestVisible = true;
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        long now = Stopwatch.GetTimestamp();
        double dt = (double)(now - _lastTimestamp) / Stopwatch.Frequency;
        _lastTimestamp = now;

        _currentVisualOffsetVertical = MathExtension.Clamp(_verticalScrollPhysics.Update(_currentVisualOffsetVertical, dt), 0, ScrollableHeight);
        _currentVisualOffsetHorizontal = MathExtension.Clamp(_horizontalScrollPhysics.Update(_currentVisualOffsetHorizontal, dt), 0, ScrollableWidth);

        if (_verticalScrollPhysics.IsStable && _horizontalScrollPhysics.IsStable)
        {
            StopRendering();
            return;
        }

        double deltaVerticalForLogicalUpdate = Math.Abs(_currentVisualOffsetVertical - _lastLogicalSyncVertical);
        double deltaHorizontalForLogicalUpdate = Math.Abs(_currentVisualOffsetHorizontal - _lastLogicalSyncHorizontal);

        _logicalOffsetUpdateAccumulatorVertical += deltaVerticalForLogicalUpdate;
        _logicalOffsetUpdateAccumulatorHorizontal += deltaHorizontalForLogicalUpdate;

        if (_logicalOffsetUpdateAccumulatorVertical >= LogicalOffsetUpdateDistanceThreshold)
        {
            _logicalOffsetUpdateAccumulatorVertical = 0;
            ScrollToVerticalOffset(_currentVisualOffsetVertical);
            _lastLogicalSyncVertical = _currentVisualOffsetVertical;
        }

        if (_logicalOffsetUpdateAccumulatorHorizontal >= LogicalOffsetUpdateDistanceThreshold)
        {
            _logicalOffsetUpdateAccumulatorHorizontal = 0;
            ScrollToHorizontalOffset(_currentVisualOffsetHorizontal);
            _lastLogicalSyncHorizontal = _currentVisualOffsetHorizontal;
        }

        _visualDeltaVertical = _logicalOffsetVertical - _currentVisualOffsetVertical;
        if (Math.Abs(_visualDeltaVertical - _lastRenderedOffsetVertical) >= VisualUpdateStepThreshold)
        {
            _transform!.Y = _lastRenderedOffsetVertical = _visualDeltaVertical;
            _PART_VerticalScrollBar?.Value = _currentVisualOffsetVertical;
        }

        _visualDeltaHorizontal =  _logicalOffsetHorizontal - _currentVisualOffsetHorizontal;
        if (Math.Abs(_visualDeltaHorizontal - _lastRenderedOffsetHorizontal) >= VisualUpdateStepThreshold)
        {
            _transform!.X = _lastRenderedOffsetHorizontal = _visualDeltaHorizontal;
            _PART_HorizontalScrollBar?.Value = _currentVisualOffsetHorizontal;
        }
    }

    #endregion

    #region Helpers & Properties

    /// <summary>
    /// Gets a value indicating whether the control can scroll vertically.
    /// 获取一个值，指示控件是否可以垂直滚动。
    /// </summary>
    public bool CanScrollVertical
    {
        get
        {
            if (ScrollInfo is IScrollInfo scrollInfo)
            {
                return scrollInfo.ExtentHeight - scrollInfo.ViewportHeight > 0;
            }

            return ExtentHeight > ViewportHeight;
        }
    }

    /// <summary>
    /// Gets a value indicating whether the control can scroll horizontally.
    /// 获取一个值，指示控件是否可以水平滚动。
    /// </summary>
    public bool CanScrollHorizontal
    {
        get
        {
            if (ScrollInfo is IScrollInfo scrollInfo)
            {
                return scrollInfo.ExtentWidth - scrollInfo.ViewportWidth > 0;
            }

            return ExtentWidth - ViewportWidth > 0;
        }
    }

    /// <summary>
    /// Gets a value indicating whether the control can scroll up.
    /// 获取一个值，指示控件是否可以向上滚动。
    /// </summary>
    public bool CanScrollUp
    {
        get
        {
            if (ScrollInfo is IScrollInfo scrollInfo)
            {
                return scrollInfo.VerticalOffset > 0.5;
            }

            return VerticalOffset > 0.5;
        }
    }

    /// <summary>
    /// Gets a value indicating whether the control can scroll down.
    /// 获取一个值，指示控件是否可以向下滚动。
    /// </summary>
    public bool CanScrollDown
    {
        get
        {
            if (ScrollInfo is IScrollInfo scrollInfo)
            {
                return scrollInfo.VerticalOffset + scrollInfo.ViewportHeight < scrollInfo.ExtentHeight - 0.5;
            }

            return VerticalOffset + ViewportHeight < ExtentHeight - 0.5;
        }
    }

    /// <summary>
    /// Gets a value indicating whether the control can scroll left.
    /// 获取一个值，指示控件是否可以向左滚动。
    /// </summary>
    public bool CanScrollLeft
    {
        get
        {
            if (ScrollInfo is IScrollInfo scrollInfo)
            {
                return scrollInfo.HorizontalOffset > 0.5;
            }

            return HorizontalOffset > 0.5;
        }
    }

    /// <summary>
    /// Gets a value indicating whether the control can scroll right.
    /// 获取一个值，指示控件是否可以向右滚动。
    /// </summary>
    public bool CanScrollRight
    {
        get
        {
            if (ScrollInfo is IScrollInfo scrollInfo)
            {
                return scrollInfo.HorizontalOffset + scrollInfo.ViewportWidth < scrollInfo.ExtentWidth - 0.5;
            }

            return HorizontalOffset + ViewportWidth < ExtentWidth - 0.5;
        }
    }



    /// <summary>
    /// Gets or sets a value indicating whether smooth manipulation (touch scrolling) is enabled.
    /// 获取或设置一个值，指示是否启用平滑操作（触摸滚动）。
    /// </summary>
    public bool IsEnableSmoothManipulating
    {
        get { return (bool)GetValue(IsEnableSmoothManipulatingProperty); }
        set { SetValue(IsEnableSmoothManipulatingProperty, value); }
    }

    /// <summary>
    /// Identifies the <see cref="IsEnableSmoothManipulating"/> dependency property.
    /// 标识 <see cref="IsEnableSmoothManipulating"/> 依赖属性。
    /// </summary>
    public static readonly DependencyProperty IsEnableSmoothManipulatingProperty =
        DependencyProperty.Register(nameof(IsEnableSmoothManipulating), typeof(bool), typeof(SmoothScrollViewer), new PropertyMetadata(false,OnIsEnableSmoothManipulatingChanged));

    private static void OnIsEnableSmoothManipulatingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if(d is SmoothScrollViewer{ } sv)
        {
            sv.IsManipulationEnabled = e.NewValue is true;
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether smooth scrolling is enabled.
    /// 获取或设置一个值，指示是否启用平滑滚动。
    /// </summary>
    /// <remarks>
    /// When false, the control falls back to standard ScrollViewer behavior.
    /// 为 false 时，控件回退到标准 ScrollViewer 行为。
    /// </remarks>
    public bool IsEnableSmoothScrolling
    {
        get { return (bool)GetValue(IsEnableSmoothScrollingProperty); }
        set { SetValue(IsEnableSmoothScrollingProperty, value); }
    }

    /// <summary>
    /// Identifies the <see cref="IsEnableSmoothScrolling"/> dependency property.
    /// 标识 <see cref="IsEnableSmoothScrolling"/> 依赖属性。
    /// </summary>
    public static readonly DependencyProperty IsEnableSmoothScrollingProperty =
        DependencyProperty.Register(nameof(IsEnableSmoothScrolling), typeof(bool), typeof(SmoothScrollViewer), new PropertyMetadata(true, OnIsEnableSmoothScrollingChanged));

    private static void OnIsEnableSmoothScrollingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SmoothScrollViewer scrollViewer && e.NewValue is bool enabled)
        {
            if (!enabled && scrollViewer._isRendering)
            {
                // Stop smooth scrolling immediately if disabled during animation
                scrollViewer.StopRendering();
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the preferred scroll orientation can be toggled by holding the Shift key.
    /// 获取或设置一个值，指示是否可以通过按住 Shift 键切换首选滚动方向。
    /// </summary>
    public bool AllowTogglePreferredScrollOrientationByShiftKey
    {
        get { return (bool)GetValue(AllowTogglePreferredScrollOrientationByShiftKeyProperty); }
        set { SetValue(AllowTogglePreferredScrollOrientationByShiftKeyProperty, value); }
    }

    /// <summary>
    /// Gets or sets the preferred scroll orientation (Vertical or Horizontal).
    /// 获取或设置首选滚动方向（垂直或水平）。
    /// </summary>
    public Orientation PreferredScrollOrientation
    {
        get { return (Orientation)GetValue(PreferredScrollOrientationProperty); }
        set { SetValue(PreferredScrollOrientationProperty, value); }
    }

    /// <summary>
    /// Identifies the <see cref="AllowTogglePreferredScrollOrientationByShiftKey"/> dependency property.
    /// 标识 <see cref="AllowTogglePreferredScrollOrientationByShiftKey"/> 依赖属性。
    /// </summary>
    public static readonly DependencyProperty AllowTogglePreferredScrollOrientationByShiftKeyProperty =
        DependencyProperty.Register(nameof(AllowTogglePreferredScrollOrientationByShiftKey), typeof(bool), typeof(SmoothScrollViewer), new FrameworkPropertyMetadata(true));

    /// <summary>
    /// Identifies the <see cref="PreferredScrollOrientation"/> dependency property.
    /// 标识 <see cref="PreferredScrollOrientation"/> 依赖属性。
    /// </summary>
    public static readonly DependencyProperty PreferredScrollOrientationProperty =
        DependencyProperty.Register(nameof(PreferredScrollOrientation), typeof(Orientation), typeof(SmoothScrollViewer), new FrameworkPropertyMetadata(Orientation.Vertical));

    /// <summary>
    /// Finds the nearest SmoothScrollViewer parent that can scroll horizontally.
    /// </summary>
    private static SmoothScrollViewer? FindParentSmoothScrollViewer(DependencyObject element)
    {
        DependencyObject? current = element;
        while (current != null)
        {
            if (current is SmoothScrollViewer ssv && ssv.CanScrollHorizontal && ssv.IsEnableSmoothScrolling)
            {
                return ssv;
            }
            current = VisualTreeHelper.GetParent(current);
        }
        return null;
    }
    #endregion
}

using FluentWpfCore.Helpers;
using FluentWpfCore.ScrollPhysics;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace FluentWpfCore.Controls;

/// <inheritdoc/>
public class SmoothScrollViewer : ScrollViewer
{
    private const double ScrollBarUpdateInterval = 1.0 / 24.0; // 24Hz for ScrollBar updates

    private double _logicalOffsetVertical;   // The actual ScrollViewer vertical offset
    private double _currentVisualOffsetVertical; // The target visual vertical offset (smooth)
    private double _visualDeltaVertical;     // Visual vertical offset delta from logical offset

    private double _logicalOffsetHorizontal;   // The actual ScrollViewer horizontal offset
    private double _currentVisualOffsetHorizontal; // The target visual horizontal offset (smooth)
    private double _visualDeltaHorizontal;     // Visual horizontal offset delta from logical offset

    private long _lastTimestamp;
    private double _scrollBarUpdateAccumulator;
    private bool _isRendering;

    private TranslateTransform? _transform;
    private UIElement? _content;

    private int _lastScrollDelta;
    private int _lastScrollingTick;
    
    private Orientation _activeScrollOrientation = Orientation.Vertical;

    public IScrollPhysics Physics { get; set; } = new DefaultScrollPhysics();

    public SmoothScrollViewer()
    {
        IsManipulationEnabled = true;

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    #region Lifecycle

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
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        StopRendering();
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

        // Lock logical offset at scroll start
        if (!_isRendering)
        {
            _logicalOffsetVertical = VerticalOffset;
            _currentVisualOffsetVertical = _logicalOffsetVertical;
            _visualDeltaVertical = 0;

            _logicalOffsetHorizontal = HorizontalOffset;
            _currentVisualOffsetHorizontal = _logicalOffsetHorizontal;
            _visualDeltaHorizontal = 0;
            
            _activeScrollOrientation = effectiveOrientation;
        }

        bool isPrecision = IsTouchpadScroll(e, out int intervalMs);

        if (effectiveOrientation == Orientation.Vertical && CanScrollVertical)
        {
            _activeScrollOrientation = Orientation.Vertical;
            Physics.OnScroll(_currentVisualOffsetVertical, e.Delta, isPrecision, 0, ScrollableHeight, intervalMs);
        }
        else if (effectiveOrientation == Orientation.Horizontal && CanScrollHorizontal)
        {
            _activeScrollOrientation = Orientation.Horizontal;
            Physics.OnScroll(_currentVisualOffsetHorizontal, e.Delta, isPrecision, 0, ScrollableWidth, intervalMs);
        }

        StartRendering();
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

            if (_isRendering && _activeScrollOrientation == Orientation.Vertical)
            {
                // Maintain visual position by adjusting transform
                _visualDeltaVertical = _currentVisualOffsetVertical - _logicalOffsetVertical;
                _transform!.Y = -_visualDeltaVertical;
            }
            else
            {
                // If not rendering or not active orientation (e.g. scrollbar drag), reset transform
                _visualDeltaVertical = 0;
                if (_transform != null)
                {
                    _transform.Y = 0;
                }
            }
        }

        if (hasHorizontalChange)
        {
            _logicalOffsetHorizontal = e.HorizontalOffset;

            if (_isRendering && _activeScrollOrientation == Orientation.Horizontal)
            {
                // Maintain visual position by adjusting transform
                _visualDeltaHorizontal = _currentVisualOffsetHorizontal - _logicalOffsetHorizontal;
                _transform!.X = -_visualDeltaHorizontal;
            }
            else
            {
                // If not rendering or not active orientation (e.g. scrollbar drag), reset transform
                _visualDeltaHorizontal = 0;
                if (_transform != null)
                {
                    _transform.X = 0;
                }
            }
        }
    }

    #endregion

    #region Rendering Loop

    private void StartRendering()
    {
        if (_isRendering) return;

        _lastTimestamp = Stopwatch.GetTimestamp();
        _scrollBarUpdateAccumulator = 0;
        CompositionTarget.Rendering += OnRendering;
        _isRendering = true;
        _content!.IsHitTestVisible = false;
    }

    private void StopRendering()
    {
        if (!_isRendering) return;

        CompositionTarget.Rendering -= OnRendering;
        _isRendering = false;

        // Final settlement: sync logical offset for active orientation
        if (_activeScrollOrientation == Orientation.Vertical)
        {
#if NET5_0_OR_GREATER
            double finalOffsetVertical = Math.Clamp(_currentVisualOffsetVertical, 0, ScrollableHeight);
#else
            double finalOffsetVertical = MathExtension.Clamp(_currentVisualOffsetVertical, 0, ScrollableHeight);
#endif
            ScrollToVerticalOffset(finalOffsetVertical);
            _visualDeltaVertical = 0;
            _logicalOffsetVertical = finalOffsetVertical;
            _transform!.Y = 0;
        }
        else
        {
#if NET5_0_OR_GREATER
            double finalOffsetHorizontal = Math.Clamp(_currentVisualOffsetHorizontal, 0, ScrollableWidth);
#else
            double finalOffsetHorizontal = MathExtension.Clamp(_currentVisualOffsetHorizontal, 0, ScrollableWidth);
#endif
            ScrollToHorizontalOffset(finalOffsetHorizontal);
            _visualDeltaHorizontal = 0;
            _logicalOffsetHorizontal = finalOffsetHorizontal;
            _transform!.X = 0;
        }
        
        _content!.IsHitTestVisible = true;
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        long now = Stopwatch.GetTimestamp();
        double dt = (double)(now - _lastTimestamp) / Stopwatch.Frequency;
        _lastTimestamp = now;

        if (_activeScrollOrientation == Orientation.Vertical)
        {
            _currentVisualOffsetVertical = Physics.Update(_currentVisualOffsetVertical, dt, 0, ScrollableHeight);
        }
        else
        {
            _currentVisualOffsetHorizontal = Physics.Update(_currentVisualOffsetHorizontal, dt, 0, ScrollableWidth);
        }

        if (Physics.IsStable)
        {
            StopRendering();
            return;
        }

        _scrollBarUpdateAccumulator += dt;
        if (_scrollBarUpdateAccumulator >= ScrollBarUpdateInterval)
        {
            _scrollBarUpdateAccumulator = 0;

            // Sync logical offset to trigger layout update (allowing virtualization)
            if (_activeScrollOrientation == Orientation.Vertical)
            {
                ScrollToVerticalOffset(_currentVisualOffsetVertical);
            }
            else
            {
                ScrollToHorizontalOffset(_currentVisualOffsetHorizontal);
            }
        }

        // Always update transform to match current visual offset relative to actual logical offset
        if (_activeScrollOrientation == Orientation.Vertical)
        {
            _visualDeltaVertical = _currentVisualOffsetVertical - _logicalOffsetVertical;
            _transform!.Y = -_visualDeltaVertical;
        }
        else
        {
            _visualDeltaHorizontal = _currentVisualOffsetHorizontal - _logicalOffsetHorizontal;
            _transform!.X = -_visualDeltaHorizontal;
        }
    }

    #endregion

    #region Helpers

    private bool IsTouchpadScroll(MouseWheelEventArgs e, out int intervalMs)
    {
        intervalMs = Environment.TickCount - _lastScrollingTick;
        var isTouchpadScrolling =
            e.Delta % Mouse.MouseWheelDeltaForOneLine != 0 ||
            (intervalMs < 100 &&
             _lastScrollDelta % Mouse.MouseWheelDeltaForOneLine != 0);

        _lastScrollDelta = e.Delta;
        _lastScrollingTick = e.Timestamp;
        return isTouchpadScrolling;
    }

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



    public bool IsEnableSmoothScrolling
    {
        get { return (bool)GetValue(IsEnableSmoothScrollingProperty); }
        set { SetValue(IsEnableSmoothScrollingProperty, value); }
    }

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

    public bool AllowTogglePreferredScrollOrientationByShiftKey
    {
        get { return (bool)GetValue(AllowTogglePreferredScrollOrientationByShiftKeyProperty); }
        set { SetValue(AllowTogglePreferredScrollOrientationByShiftKeyProperty, value); }
    }

    public Orientation PreferredScrollOrientation
    {
        get { return (Orientation)GetValue(PreferredScrollOrientationProperty); }
        set { SetValue(PreferredScrollOrientationProperty, value); }
    }

    public static readonly DependencyProperty AllowTogglePreferredScrollOrientationByShiftKeyProperty =
        DependencyProperty.Register(nameof(AllowTogglePreferredScrollOrientationByShiftKey), typeof(bool), typeof(SmoothScrollViewer), new FrameworkPropertyMetadata(true));

    public static readonly DependencyProperty PreferredScrollOrientationProperty =
        DependencyProperty.Register(nameof(PreferredScrollOrientation), typeof(Orientation), typeof(SmoothScrollViewer), new FrameworkPropertyMetadata(Orientation.Vertical));


    #endregion
}

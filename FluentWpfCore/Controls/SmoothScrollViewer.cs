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

/// <inheritdoc/>
public class SmoothScrollViewer : ScrollViewer
{
    private const double LogicalOffsetUpdateInterval = 1.0 / 24.0; // 24Hz for ScrollBar updates
    private const int WM_MOUSEHWHEEL = 0x020E; // Horizontal mouse wheel message

    private double _logicalOffsetVertical;   // The actual ScrollViewer vertical offset
    private double _currentVisualOffsetVertical; // The target visual vertical offset (smooth)
    private double _visualDeltaVertical;     // Visual vertical offset delta from logical offset

    private double _logicalOffsetHorizontal;   // The actual ScrollViewer horizontal offset
    private double _currentVisualOffsetHorizontal; // The target visual horizontal offset (smooth)
    private double _visualDeltaHorizontal;     // Visual horizontal offset delta from logical offset

    private long _lastTimestamp;
    private double _logicalOffsetUpdateAccumulator;
    private bool _isRendering;

    private TranslateTransform? _transform;
    private UIElement? _content;
    private ScrollBar? _PART_VerticalScrollBar, _PART_HorizontalScrollBar;
    private HwndSource? _hwndSource;

    private Orientation _activeScrollOrientation = Orientation.Vertical;

    private IScrollPhysics _verticalScrollPhysics = new DefaultScrollPhysics();
    private IScrollPhysics _horizontalScrollPhysics = new DefaultScrollPhysics();

    public IScrollPhysics Physics
    {
        get => _verticalScrollPhysics;
        set => _horizontalScrollPhysics = _verticalScrollPhysics = value;
    }

    public SmoothScrollViewer()
    {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
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
                            HandleScroll(-delta, Orientation.Horizontal);
                            handled = true;
                        }
                    }
                }
            }
        }
        return IntPtr.Zero;
    }


    private void HandleScroll(int delta, Orientation orientation)
    {
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

        _activeScrollOrientation = orientation;

        if (orientation == Orientation.Vertical)
        {
            _verticalScrollPhysics.OnScroll(delta);
        }
        else
        {
            _horizontalScrollPhysics.OnScroll(delta);
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

        if (effectiveOrientation == Orientation.Vertical && CanScrollVertical)
        {
            HandleScroll(e.Delta, Orientation.Vertical);
        }
        else if (effectiveOrientation == Orientation.Horizontal && CanScrollHorizontal)
        {
            HandleScroll(e.Delta, Orientation.Horizontal);
        }
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
        _logicalOffsetUpdateAccumulator = 0;
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
            //{TemplateBinding VerticalOffset}
            _PART_VerticalScrollBar?.SetBinding(ScrollBar.ValueProperty,new Binding("VerticalOffset") { RelativeSource=new RelativeSource(RelativeSourceMode.TemplatedParent),Mode=BindingMode.OneWay});
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
            _PART_HorizontalScrollBar?.SetBinding(ScrollBar.ValueProperty,new Binding("HorizontalOffset") { RelativeSource=new RelativeSource(RelativeSourceMode.TemplatedParent),Mode=BindingMode.OneWay});
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
            _currentVisualOffsetVertical = MathExtension.Clamp(_verticalScrollPhysics.Update(_currentVisualOffsetVertical, dt), 0, ScrollableHeight);
        }
        else
        {
            _currentVisualOffsetHorizontal = MathExtension.Clamp(_horizontalScrollPhysics.Update(_currentVisualOffsetHorizontal, dt), 0, ScrollableWidth);
        }

        if (_verticalScrollPhysics.IsStable&& _horizontalScrollPhysics.IsStable)
        {
            StopRendering();
            return;
        }

        _logicalOffsetUpdateAccumulator += dt;
        if (_logicalOffsetUpdateAccumulator >= LogicalOffsetUpdateInterval)
        {
            _logicalOffsetUpdateAccumulator = 0;

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
            _PART_VerticalScrollBar?.Value = _currentVisualOffsetVertical;
        }
        else
        {
            _visualDeltaHorizontal = _currentVisualOffsetHorizontal - _logicalOffsetHorizontal;
            _transform!.X = -_visualDeltaHorizontal;
            _PART_HorizontalScrollBar?.Value = _currentVisualOffsetHorizontal;
        }
    }

    #endregion

    #region Helpers & Properties

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

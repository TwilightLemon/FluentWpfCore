using FluentWpfCore.Helpers;
using FluentWpfCore.ScrollPhysics;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace FluentWpfCore.Controls;
public class SmoothScrollViewer : ScrollViewer
{
    private const double ScrollBarUpdateInterval = 1.0 / 24.0; // 24Hz for ScrollBar updates

    private double _logicalOffset;   // The actual ScrollViewer offset
    private double _currentVisualOffset; // The target visual offset (smooth)
    private double _visualDelta;     // Visual offset delta from logical offset

    private long _lastTimestamp;
    private double _scrollBarUpdateAccumulator;
    private bool _isRendering;

    private TranslateTransform? _transform;
    private UIElement? _content;

    private int _lastScrollDelta;
    private int _lastScrollingTick;

    public IScrollPhysics Physics { get; set; } = new DefaultScrollPhysics();

    public SmoothScrollViewer()
    {
        IsManipulationEnabled = true;
        PanningMode = PanningMode.VerticalOnly;

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
            throw new NotImplementedException("SmoothScrollViewer.Content is not a UIElement");
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        StopRendering();
    }

    #endregion

    #region Input

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        e.Handled = true;

        // Lock logical offset at scroll start
        if (!_isRendering)
        {
            _logicalOffset = VerticalOffset;
            _currentVisualOffset = _logicalOffset;
            _visualDelta = 0;
        }

        bool isPrecision = IsTouchpadScroll(e);
        Physics.OnScroll(_currentVisualOffset, e.Delta, isPrecision, 0, ScrollableHeight);

        StartRendering();
    }

    protected override void OnScrollChanged(ScrollChangedEventArgs e)
    {
        base.OnScrollChanged(e);
        
        if (e.VerticalChange == 0) return;

        _logicalOffset = e.VerticalOffset;

        if (_isRendering)
        {
            // Maintain visual position by adjusting transform
            _visualDelta = _currentVisualOffset - _logicalOffset;
            _transform!.Y = -_visualDelta;
        }
        else
        {
            // If not rendering (e.g. scrollbar drag), reset transform
            _visualDelta = 0;
            _transform!.Y = 0;
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
        if (_content != null)
            _content.IsHitTestVisible = false;
    }

    private void StopRendering()
    {
        if (!_isRendering) return;

        CompositionTarget.Rendering -= OnRendering;
        _isRendering = false;

        // Final settlement: sync logical offset
#if NET5_0_OR_GREATER
        double finalOffset = Math.Clamp(_currentVisualOffset, 0, ScrollableHeight);
#else
        double finalOffset = MathExtension.Clamp(_currentVisualOffset, 0, ScrollableHeight);
#endif
        ScrollToVerticalOffset(finalOffset);

        // Clear visual delta and reset transform
        _visualDelta = 0;
        _logicalOffset = finalOffset;
        _transform!.Y = 0;
        _content!.IsHitTestVisible = true;
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        long now = Stopwatch.GetTimestamp();
        double dt = (double)(now - _lastTimestamp) / Stopwatch.Frequency;
        _lastTimestamp = now;

        _currentVisualOffset = Physics.Update(_currentVisualOffset, dt, 0, ScrollableHeight);

        if (Physics.IsStable)
        {
            StopRendering();
            return;
        }

        // Update ScrollBar thumb
        _scrollBarUpdateAccumulator += dt;
        if (_scrollBarUpdateAccumulator >= ScrollBarUpdateInterval)
        {
            _scrollBarUpdateAccumulator = 0;

            // Sync logical offset to trigger layout update (allowing virtualization)
            ScrollToVerticalOffset(_currentVisualOffset);
        }
        
        // Always update transform to match current visual offset relative to actual logical offset
        _visualDelta = _currentVisualOffset - _logicalOffset;
        _transform!.Y = -_visualDelta;
    }

#endregion

    #region Helpers

    private bool IsTouchpadScroll(MouseWheelEventArgs e)
    {
        var tickCount = Environment.TickCount;
        var isTouchpadScrolling =
            e.Delta % Mouse.MouseWheelDeltaForOneLine != 0 ||
            (tickCount - _lastScrollingTick < 100 &&
             _lastScrollDelta % Mouse.MouseWheelDeltaForOneLine != 0);

        _lastScrollDelta = e.Delta;
        _lastScrollingTick = e.Timestamp;
        return isTouchpadScrolling;
    }

    #endregion
}

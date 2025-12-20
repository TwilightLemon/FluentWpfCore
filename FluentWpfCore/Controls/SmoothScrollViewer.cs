using FluentWpfCore.Helpers;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace FluentWpfCore.Controls;

public class SmoothScrollViewer : ScrollViewer
{
    private const double ScrollBarUpdateInterval = 1.0 / 24.0; // 24Hz for ScrollBar updates

    private double _logicalOffset;   // The actual ScrollViewer offset
    private double _visualDelta;     // Visual offset delta from logical offset

    private long _lastTimestamp;
    private double _scrollBarUpdateAccumulator;
    private bool _isRendering;

    private TranslateTransform? _transform;

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
            _transform = new TranslateTransform();
            element.RenderTransform = _transform;
            element.RenderTransformOrigin = new Point(0, 0);
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
            _visualDelta = 0;
        }

        bool isPrecision = IsTouchpadScroll(e);
        Physics.OnScroll(_logicalOffset + _visualDelta, e.Delta, isPrecision, 0, ScrollableHeight);

        StartRendering();
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
    }

    private void StopRendering()
    {
        if (!_isRendering) return;

        CompositionTarget.Rendering -= OnRendering;
        _isRendering = false;

        // Final settlement: sync logical offset
        double finalOffset = Clamp(_logicalOffset + _visualDelta, 0, ScrollableHeight);
        ScrollToVerticalOffset(finalOffset);

        // Clear visual delta and reset transform
        _visualDelta = 0;
        _logicalOffset = finalOffset;
        _transform?.Y = 0;
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        long now = Stopwatch.GetTimestamp();
        double dt = (double)(now - _lastTimestamp) / Stopwatch.Frequency;
        _lastTimestamp = now;

        double currentVisualOffset = _logicalOffset + _visualDelta;
        double newVisualOffset = Physics.Update(currentVisualOffset, dt, 0, ScrollableHeight);

        if (Physics.IsStable)
        {
            _visualDelta = newVisualOffset - _logicalOffset;
            StopRendering();
            return;
        }

        // Update ScrollBar thumb at 24Hz (without triggering layout)
        _scrollBarUpdateAccumulator += dt;
        if (_scrollBarUpdateAccumulator >= ScrollBarUpdateInterval)
        {
            _scrollBarUpdateAccumulator = 0;
            
            // Sync logical offset to trigger virtualization
            ScrollToVerticalOffset(newVisualOffset);
            _logicalOffset = newVisualOffset;
            _visualDelta = 0;
            _transform?.Y = 0;
        }
        else
        {
            _visualDelta = newVisualOffset - _logicalOffset;
            _transform?.Y = -_visualDelta;
        }
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

    //For .net 4.5 compatibility
    private static double Clamp(double value, double min, double max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }

    #endregion
}

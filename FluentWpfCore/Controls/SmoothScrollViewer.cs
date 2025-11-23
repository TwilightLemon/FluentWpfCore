using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace FluentWpfCore.Controls;

#if !NET5_0_OR_GREATER
internal static class MathExtensions
{
    public static double Clamp(double value, double min, double max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }
}
#endif

/// <summary>
/// 带平滑滚动效果的 ScrollViewer，支持触摸板和鼠标滚轮的惯性滚动 [Vertical Only]
/// </summary>
public class SmoothScrollViewer : ScrollViewer
{
    #region Model Parameters

    /// <summary>
    /// 缓动模型的叠加速度力度，数值越大，滚动起始速率越快，滚得越远
    /// </summary>
    private const double VelocityFactor = 2.0;

    /// <summary>
    /// 缓动模型的速度衰减系数，数值越小，越快停下来
    /// </summary>
    private const double Friction = 0.92;

    /// <summary>
    /// 精确模型的插值系数，数值越大，滚动越快接近目标
    /// </summary>
    private const double LerpFactor = 0.5;

    /// <summary>
    /// 目标帧时间
    /// </summary>
    private const double TargetFrameTime = 1.0d / 144;

    #endregion

    #region Fields

    private int _lastScrollingTick = 0;
    private int _lastScrollDelta = 0;
    private double _targetOffset = 0;
    private double _targetVelocity = 0;
    private long _lastTimestamp = 0;
    private bool _isRenderingHooked = false;
    private bool _isAccuracyControl = false;

    #endregion

    public SmoothScrollViewer()
    {
        IsManipulationEnabled = true;
        PanningMode = PanningMode.VerticalOnly;
        Unloaded += ScrollViewer_Unloaded;
    }

    #region Event Handlers

    private void ScrollViewer_Unloaded(object sender, RoutedEventArgs e)
    {
        if (_isRenderingHooked)
        {
            CompositionTarget.Rendering -= OnRendering;
            _isRenderingHooked = false;
        }
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        e.Handled = true;

        _isAccuracyControl = IsTouchpadScroll(e);

        if (_isAccuracyControl)
        {
            _targetVelocity = 0;
#if NET5_0_OR_GREATER
            _targetOffset = Math.Clamp(VerticalOffset - e.Delta, 0, ScrollableHeight);
#else
            _targetOffset = MathExtensions.Clamp(VerticalOffset - e.Delta, 0, ScrollableHeight);
#endif
        }
        else
        {
            _targetVelocity += -e.Delta * VelocityFactor;
        }

        if (!_isRenderingHooked)
        {
            _lastTimestamp = Stopwatch.GetTimestamp();
            CompositionTarget.Rendering += OnRendering;
            _isRenderingHooked = true;
        }
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        long currentTimestamp = Stopwatch.GetTimestamp();
        double deltaTime = (double)(currentTimestamp - _lastTimestamp) / Stopwatch.Frequency;
        _lastTimestamp = currentTimestamp;

        double timeFactor = deltaTime / TargetFrameTime;
        double currentOffset = VerticalOffset;

        if (_isAccuracyControl)
        {
            double lerpAmount = 1.0 - Math.Pow(1.0 - LerpFactor, timeFactor);
            currentOffset += (_targetOffset - currentOffset) * lerpAmount;

            if (Math.Abs(_targetOffset - currentOffset) < 0.5)
            {
                currentOffset = _targetOffset;
                StopRendering();
            }
        }
        else
        {
            if (Math.Abs(_targetVelocity) < 0.1)
            {
                _targetVelocity = 0;
                StopRendering();
                return;
            }

            _targetVelocity *= Math.Pow(Friction, timeFactor);
#if NET5_0_OR_GREATER
            currentOffset = Math.Clamp(currentOffset + _targetVelocity * (timeFactor / 24), 0, ScrollableHeight);
#else
            currentOffset = MathExtensions.Clamp(currentOffset + _targetVelocity * (timeFactor / 24), 0, ScrollableHeight);
#endif
        }

        ScrollToVerticalOffset(currentOffset);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// 判断 MouseWheel 事件由鼠标触发还是由触控板触发
    /// </summary>
    private bool IsTouchpadScroll(MouseWheelEventArgs e)
    {
        var tickCount = Environment.TickCount;
        var isTouchpadScrolling =
            e.Delta % Mouse.MouseWheelDeltaForOneLine != 0 ||
            (tickCount - _lastScrollingTick < 100 && _lastScrollDelta % Mouse.MouseWheelDeltaForOneLine != 0);

        _lastScrollDelta = e.Delta;
        _lastScrollingTick = e.Timestamp;
        return isTouchpadScrolling;
    }

    private void StopRendering()
    {
        CompositionTarget.Rendering -= OnRendering;
        _isRenderingHooked = false;
    }

    #endregion
}

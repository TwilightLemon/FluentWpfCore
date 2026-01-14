using FluentWpfCore.Helpers;
using System.ComponentModel;

namespace FluentWpfCore.ScrollPhysics;

/// <summary>
/// Exponential scroll physics implementation using exponential decay function.
/// 基于指数函数的缓动滚动物理模型。
/// </summary>
/// <remarks>
/// Uses offset = target + (start - target) * e^(-k*t) to achieve smooth scrolling.
/// Characteristics: fast start, slow end, natural deceleration feel.
/// 使用 offset = target + (start - target) * e^(-k*t) 实现平滑滚动。
/// 特点：开始快、结束慢，自然减速感。
/// </remarks>
public class ExponentialScrollPhysics : IScrollPhysics
{
    private double _decayRate = 8.0;

    /// <summary>
    /// Gets or sets the decay rate. Higher values result in faster arrival at the target position.
    /// 获取或设置衰减速率。数值越大，滚动越快到达目标位置。
    /// </summary>
    /// <remarks>
    /// Valid range: 1 to 20.
    /// 有效范围：1 到 20。
    /// </remarks>
    [Category("Scroll Physics")]
    [Description("衰减速率，数值越大，滚动越快到达目标位置。取值1~20")]
    public double DecayRate
    {
        get => _decayRate;
        set
        {
#if NET5_0_OR_GREATER
            _decayRate = Math.Clamp(value, 1.0, 20.0);
#else
            _decayRate = MathExtension.Clamp(value, 1.0, 20.0);
#endif
        }
    }

    /// <summary>
    /// Gets or sets the stop threshold. Scrolling stops when the remaining distance is below this value.
    /// 获取或设置停止阈值。当剩余距离小于此值时停止滚动。
    /// </summary>
    /// <remarks>
    /// Valid range: 0.1 to 5.
    /// 有效范围：0.1 到 5。
    /// </remarks>
    [Category("Scroll Physics")]
    [Description("停止阈值，当剩余距离小于此值时停止滚动。取值0.1~5")]
    public double StopThreshold { get; set; } = 0.5;

    // Remaining distance to scroll (negative = scroll up, positive = scroll down)
    // 剩余需要滚动的距离（负值向上滚动，正值向下滚动）
    private double _remainingDistance;
    private bool _isStable = true;

    /// <summary>
    /// Gets a value indicating whether the scrolling has stabilized.
    /// 获取一个值，指示滚动是否已稳定。
    /// </summary>
    public bool IsStable => _isStable;

    /// <summary>
    /// Gets or sets a value indicating whether precise mode is enabled.
    /// 获取或设置一个值，指示是否启用精确模式。
    /// </summary>
    public bool IsPreciseMode { get; set; } = false;

    /// <inheritdoc/>
    public void OnScroll(double delta)
    {
        _isStable = false;
        // 累加剩余距离，支持连续滚动
        _remainingDistance -= delta;
    }

    /// <inheritdoc/>
    public double Update(double currentOffset, double dt)
    {
        if (_isStable) return currentOffset;

        if (Math.Abs(_remainingDistance) < StopThreshold)
        {
            double finalDisplacement = _remainingDistance;
            _remainingDistance = 0;
            _isStable = true;
            return currentOffset + finalDisplacement; // 直接到达目标
        }

        // 指数插值因子：1 - e^(-k*dt)
        // 每帧消耗剩余距离的一定比例
        double factor = 1.0 - Math.Exp(-_decayRate * dt);
        double displacement = _remainingDistance * factor;

        // 减少剩余距离
        _remainingDistance -= displacement;

        return currentOffset + displacement;
    }
}

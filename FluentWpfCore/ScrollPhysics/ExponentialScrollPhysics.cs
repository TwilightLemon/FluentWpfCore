using FluentWpfCore.Helpers;
using System.ComponentModel;

namespace FluentWpfCore.ScrollPhysics;

/// <summary>
/// 基于指数函数的缓动滚动物理模型
/// 使用 offset = target + (start - target) * e^(-k*t) 实现平滑滚动
/// 特点：开始快、结束慢，自然减速感
/// Exponential easing scroll physics model
/// Uses offset = target + (start - target) * e^(-k*t) for smooth scrolling
/// Characteristics: fast start, slow end, natural deceleration feel
/// </summary>
public class ExponentialScrollPhysics : IScrollPhysics
{
    private double _decayRate = 8.0;

    /// <summary>
    /// 获取或设置衰减速率（1~20），数值越大，滚动越快到达目标位置
    /// Gets or sets decay rate (1~20). Higher values reach target position faster
    /// </summary>
    [Category("Scroll Physics")]
    [Description("衰减速率，数值越大，滚动越快到达目标位置。取值1~20 / Decay rate. Higher values reach target faster. Range: 1~20")]
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
    /// 获取或设置停止阈值（0.1~5），当剩余距离小于此值时停止滚动
    /// Gets or sets stop threshold (0.1~5). Scrolling stops when remaining distance is below this value
    /// </summary>
    [Category("Scroll Physics")]
    [Description("停止阈值，当剩余距离小于此值时停止滚动。取值0.1~5 / Stop threshold. Scrolling stops when remaining distance is below this. Range: 0.1~5")]
    public double StopThreshold { get; set; } = 0.5;

    // 剩余需要滚动的距离（负值向上滚动，正值向下滚动）
    // Remaining distance to scroll (negative for upward, positive for downward)
    private double _remainingDistance;
    private bool _isStable = true;

    /// <summary>
    /// 获取滚动是否已稳定（动画结束）
    /// Gets whether scrolling is stable (animation ended)
    /// </summary>
    public bool IsStable => _isStable;

    /// <summary>
    /// 获取或设置是否为精确模式（触摸、触控笔输入）
    /// Gets or sets whether in precise mode (touch, stylus input)
    /// </summary>
    public bool IsPreciseMode { get; set; } = false;

    /// <summary>
    /// 处理滚动输入事件
    /// Handles scroll input events
    /// </summary>
    /// <param name="delta">滚动量 / Scroll delta</param>
    public void OnScroll(double delta)
    {
        _isStable = false;
        // 累加剩余距离，支持连续滚动
        // Accumulate remaining distance to support continuous scrolling
        _remainingDistance -= delta;
    }

    /// <summary>
    /// 更新滚动位置（每帧调用）
    /// Updates scroll position (called every frame)
    /// </summary>
    /// <param name="currentOffset">当前偏移量 / Current offset</param>
    /// <param name="dt">距离上一帧的时间增量 / Time delta since last frame</param>
    /// <returns>新的偏移量 / New offset</returns>
    public double Update(double currentOffset, double dt)
    {
        if (_isStable) return currentOffset;

        if (Math.Abs(_remainingDistance) < StopThreshold)
        {
            double finalDisplacement = _remainingDistance;
            _remainingDistance = 0;
            _isStable = true;
            return currentOffset + finalDisplacement; // 直接到达目标 / Directly reach target
        }

        // 指数插值因子：1 - e^(-k*dt)
        // Exponential interpolation factor: 1 - e^(-k*dt)
        // 每帧消耗剩余距离的一定比例
        // Consume a certain proportion of remaining distance each frame
        double factor = 1.0 - Math.Exp(-_decayRate * dt);
        double displacement = _remainingDistance * factor;

        // 减少剩余距离
        // Reduce remaining distance
        _remainingDistance -= displacement;

        return currentOffset + displacement;
    }
}

using FluentWpfCore.Helpers;
using System.ComponentModel;

namespace FluentWpfCore.ScrollPhysics;

/// <summary>
/// 基于指数函数的缓动滚动物理模型
/// 使用 offset = target + (start - target) * e^(-k*t) 实现平滑滚动
/// 特点：开始快、结束慢，自然减速感
/// </summary>
public class ExponentialScrollPhysics : IScrollPhysics
{
    private double _decayRate = 8.0;

    /// <summary>
    /// 衰减速率，数值越大，滚动越快到达目标位置
    /// </summary>
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
    /// 停止阈值，当剩余距离小于此值时停止滚动
    /// </summary>
    [Category("Scroll Physics")]
    [Description("停止阈值，当剩余距离小于此值时停止滚动。取值0.1~5")]
    public double StopThreshold { get; set; } = 0.5;

    // 剩余需要滚动的距离（负值向上滚动，正值向下滚动）
    private double _remainingDistance;
    private bool _isStable = true;

    public bool IsStable => _isStable;

    public void OnScroll(double delta)
    {
        _isStable = false;
        // 累加剩余距离，支持连续滚动
        _remainingDistance -= delta;
    }

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

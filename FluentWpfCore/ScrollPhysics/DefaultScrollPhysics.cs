using FluentWpfCore.Helpers;
using System.ComponentModel;

namespace FluentWpfCore.ScrollPhysics;

public class DefaultScrollPhysics : IScrollPhysics
{
    // 摩擦系数的有效范围：0.85 ~ 0.96
    // Smoothness 0 -> Friction 0.85 (快速停止)
    // Smoothness 1 -> Friction 0.96 (平滑持久)
    private const double MinFriction = 0.85;
    private const double MaxFriction = 0.96;

    private double _smoothness = 0.72;

    /// <summary>
    /// 滚动平滑度，数值越大，滚动越平滑持久；数值越小，越快停下来
    /// </summary>
    [Category("Scroll Physics")]
    [Description("滚动平滑度，数值越大，滚动越平滑持久；数值越小，越快停下来。取值0~1")]
    public double Smoothness
    {
        get => _smoothness;
        set
        {
#if NET5_0_OR_GREATER
            _smoothness = Math.Clamp(value, 0.0, 1.0);
#else
            _smoothness = MathExtension.Clamp(value, 0.0, 1.0);
#endif
        }
    }

    /// <summary>
    /// 根据 Smoothness 计算实际的摩擦系数
    /// </summary>
    private double Friction => MinFriction + _smoothness * (MaxFriction - MinFriction);


    /// <summary>
    /// 参考帧时间（用于归一化计算，与实际显示器帧率无关）
    /// 通过 timeFactor = dt / ReferenceFrameTime 实现帧率无关的物理模拟
    /// </summary>
    private const double ReferenceFrameTime = 1.0 / 144.0;

    private double _velocity;
    private bool _isStable = true;

    public bool IsStable => _isStable;

    public void OnScroll(double delta)
    {
        _isStable = false;
        // 使用目标位移模式：将 delta 累加到目标位置
        // 初始速度设为 -delta，配合位移系数 (1 - Friction)，保证总位移等于 delta
        // 总位移 = v0 * k * (1 + f + f² + ...) = v0 * k / (1 - f)
        // 当 k = (1 - f) 时，总位移 = v0 = -delta，即向上滚动 delta
        _velocity -= delta;
    }

    public double Update(double currentOffset, double dt)
    {
        if (_isStable) return currentOffset;

        // 帧率无关的时间因子：无论实际帧率如何，物理效果保持一致
        double timeFactor = dt / ReferenceFrameTime;
        double newOffset = currentOffset;


        if (Math.Abs(_velocity) < 0.5)
        {
            _velocity = 0;
            _isStable = true;
        }
        else
        {
            // 位移系数 = (1 - Friction)，保证总位移等于初始速度
            // 使用帧时间因子调整摩擦力和位移，实现帧率无关
            double friction = Math.Pow(Friction, timeFactor);
            double displacement = _velocity * (1 - friction);
            _velocity *= friction;
            newOffset = currentOffset + displacement;
        }

        return newOffset;
    }
}

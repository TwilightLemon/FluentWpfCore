using FluentWpfCore.Helpers;
using System.ComponentModel;
using System.Windows.Input;

namespace FluentWpfCore.ScrollPhysics;

/// <summary>
/// Default scroll physics implementation using velocity-based decay with friction.
/// 默认滚动物理实现，使用基于速度的摩擦衰减。
/// </summary>
/// <remarks>
/// This physics model uses velocity decay with configurable friction for natural momentum scrolling.
/// The total scroll distance equals the input delta, providing predictable scrolling behavior.
/// 此物理模型使用可配置摩擦力的速度衰减实现自然的惯性滚动。
/// 总滚动距离等于输入的 delta 值，提供可预测的滚动行为。
/// </remarks>
public class DefaultScrollPhysics : IScrollPhysics
{
    // Friction coefficient valid range: 0.85 ~ 0.96
    // 摩擦系数的有效范围：0.85 ~ 0.96
    // Smoothness 0 -> Friction 0.85 (stops quickly / 快速停止)
    // Smoothness 1 -> Friction 0.96 (smooth deceleration / 平滑减速)
    private const double MinFriction = 0.85;
    private const double MaxFriction = 0.96;
    private const double PreciseModeFriction = 0.88;

    /// <summary>
    /// Calculates the actual friction coefficient based on Smoothness.
    /// 根据 Smoothness 计算实际的摩擦系数。
    /// </summary>
    private double _friction = 0d;
    private double _smoothness = 0.72;
    private bool _isPreciseMode = false;

    public DefaultScrollPhysics()
    {
        InitParameters();
    }

    private void InitParameters()
    {
        _friction = MinFriction + (MaxFriction - MinFriction) * _smoothness;
    }

    /// <summary>
    /// Gets or sets the scroll smoothness. Higher values result in smoother, longer-lasting scrolling.
    /// 获取或设置滚动平滑度。数值越大，滚动越平滑持久。
    /// </summary>
    /// <remarks>
    /// Valid range: 0 to 1. 0 = stops quickly, 1 = very smooth deceleration.
    /// 有效范围：0 到 1。0 = 快速停止，1 = 非常平滑的减速。
    /// </remarks>
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
            InitParameters();
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether precise mode is enabled.
    /// 获取或设置一个值，指示是否启用精确模式。
    /// </summary>
    public bool IsPreciseMode
    {
        get => _isPreciseMode;
        set => _isPreciseMode = value;
    }

    /// <summary>
    /// Reference frame time used for normalization (independent of actual display refresh rate).
    /// 参考帧时间（用于归一化计算，与实际显示器帧率无关）。
    /// </summary>
    /// <remarks>
    /// Through timeFactor = dt / ReferenceFrameTime, achieves frame-rate independent physics simulation.
    /// 通过 timeFactor = dt / ReferenceFrameTime 实现帧率无关的物理模拟。
    /// </remarks>
    private const double ReferenceFrameTime = 1.0 / 144.0;

    private double _velocity;
    private bool _isStable = true;

    /// <summary>
    /// Gets a value indicating whether the scrolling has stabilized.
    /// 获取一个值，指示滚动是否已稳定。
    /// </summary>
    public bool IsStable => _isStable;

    /// <inheritdoc/>
    public void OnScroll(double delta)
    {
        _isStable = false;
        // 使用目标位移模式：将 delta 累加到目标位置
        // 初始速度设为 -delta，配合位移系数 (1 - Friction)，保证总位移等于 delta
        // 总位移 = v0 * k * (1 + f + f² + ...) = v0 * k / (1 - f)
        // 当 k = (1 - f) 时，总位移 = v0 = -delta，即向上滚动 delta
        _velocity -= delta;
    }

    /// <inheritdoc/>
    public double Update(double currentOffset, double dt)
    {
        if (_isStable) return currentOffset;

        // 帧率无关的时间因子：无论实际帧率如何，物理效果保持一致
        double timeFactor = dt / ReferenceFrameTime;
        double newOffset = currentOffset;


        if (Math.Abs(_velocity) < 0.01)
        {
            _velocity = 0;
            _isStable = true;
        }
        else
        {
            // 位移系数 = (1 - Friction)，保证总位移等于初始速度
            // 使用帧时间因子调整摩擦力和位移，实现帧率无关
            double f = Math.Pow(_isPreciseMode? PreciseModeFriction : _friction, timeFactor);
            double displacement = _velocity * (1 - f);
            _velocity -= displacement;
            newOffset = currentOffset + displacement;
        }

        return newOffset;
    }
}

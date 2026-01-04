using FluentWpfCore.Helpers;
using System.ComponentModel;
using System.Windows.Input;

namespace FluentWpfCore.ScrollPhysics;

/// <summary>
/// 基于速度衰减的默认滚动物理模型，使用摩擦力实现惯性滚动效果
/// Default scroll physics based on velocity decay, using friction to achieve inertial scrolling
/// </summary>
public class DefaultScrollPhysics : IScrollPhysics
{
    // 摩擦系数的有效范围：0.85 ~ 0.96
    // Valid range of friction coefficient: 0.85 ~ 0.96
    // Smoothness 0 -> Friction 0.85 (快速停止 / fast stop)
    // Smoothness 1 -> Friction 0.96 (缓动延迟 / smooth delay)
    private const double MinFriction = 0.85;
    private const double MaxFriction = 0.96;
    private const double PreciseModeFriction = 0.76;

    /// <summary>
    /// 根据 Smoothness 计算实际的摩擦系数
    /// Calculate actual friction coefficient based on Smoothness
    /// </summary>
    private double _friction = 0d;
    private double _smoothness = 0.72;
    private bool _isPreciseMode = false;

    /// <summary>
    /// 初始化默认滚动物理模型
    /// Initializes the default scroll physics
    /// </summary>
    public DefaultScrollPhysics()
    {
        InitParameters();
    }

    private void InitParameters()
    {
        _friction = MinFriction + (MaxFriction - MinFriction) * _smoothness;
    }

    /// <summary>
    /// 获取或设置滚动平滑度（0~1），数值越大，滚动越平滑持久；数值越小，越快停下来
    /// Gets or sets scroll smoothness (0~1). Higher values result in smoother, longer-lasting scrolling; lower values stop faster
    /// </summary>
    [Category("Scroll Physics")]
    [Description("滚动平滑度，数值越大，滚动越平滑持久；数值越小，越快停下来。取值0~1 / Scroll smoothness. Higher values result in smoother scrolling. Range: 0~1")]
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
    /// 获取或设置是否为精确模式（触摸、触控笔输入）
    /// Gets or sets whether in precise mode (touch, stylus input)
    /// </summary>
    public bool IsPreciseMode
    {
        get => _isPreciseMode;
        set => _isPreciseMode = value;
    }

    /// <summary>
    /// 参考帧时间（用于归一化计算，与实际显示器帧率无关）
    /// 通过 timeFactor = dt / ReferenceFrameTime 实现帧率无关的物理模拟
    /// Reference frame time (for normalization, independent of actual monitor refresh rate)
    /// Achieves frame-rate-independent physics simulation via timeFactor = dt / ReferenceFrameTime
    /// </summary>
    private const double ReferenceFrameTime = 1.0 / 144.0;

    private double _velocity;
    private bool _isStable = true;

    /// <summary>
    /// 获取滚动是否已稳定（动画结束）
    /// Gets whether scrolling is stable (animation ended)
    /// </summary>
    public bool IsStable => _isStable;

    /// <summary>
    /// 处理滚动输入事件
    /// Handles scroll input events
    /// </summary>
    /// <param name="delta">滚动量 / Scroll delta</param>
    public void OnScroll(double delta)
    {
        _isStable = false;
        // 使用目标位移模式：将 delta 累加到目标位置
        // Use target displacement mode: accumulate delta to target position
        // 初始速度设为 -delta，配合位移系数 (1 - Friction)，保证总位移等于 delta
        // Set initial velocity to -delta, combined with displacement coefficient (1 - Friction), ensures total displacement equals delta
        // 总位移 = v0 * k * (1 + f + f² + ...) = v0 * k / (1 - f)
        // Total displacement = v0 * k * (1 + f + f² + ...) = v0 * k / (1 - f)
        // 当 k = (1 - f) 时，总位移 = v0 = -delta，即向上滚动 delta
        // When k = (1 - f), total displacement = v0 = -delta, scrolling up by delta
        _velocity -= delta;
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

        // 帧率无关的时间因子：无论实际帧率如何，物理效果保持一致
        // Frame-rate-independent time factor: maintains consistent physics effect regardless of actual frame rate
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
            // Displacement coefficient = (1 - Friction), ensures total displacement equals initial velocity
            // 使用帧时间因子调整摩擦力和位移，实现帧率无关
            // Use frame time factor to adjust friction and displacement for frame-rate independence
            double f = Math.Pow(_isPreciseMode? PreciseModeFriction : _friction, timeFactor);
            double displacement = _velocity * (1 - f);
            _velocity *= f;
            newOffset = currentOffset + displacement;
        }

        return newOffset;
    }
}

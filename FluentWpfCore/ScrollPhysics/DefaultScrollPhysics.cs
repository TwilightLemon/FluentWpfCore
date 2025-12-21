using FluentWpfCore.Helpers;
using System.ComponentModel;
using System.Diagnostics;

namespace FluentWpfCore.ScrollPhysics;

public class DefaultScrollPhysics : IScrollPhysics
{
    /// <summary>
    /// 缓动模型的叠加速度力度，数值越大，滚动起始速率越快，滚得越远
    /// </summary>
    [Category("Scroll Physics")]
    [Description("缓动模型的叠加速度力度，数值越大，滚动起始速率越快，滚得越远。取值1~5")]
    [Obsolete("该参数不再有效，如需修改起始速率倍速因子请使用MinVelocityFactor")]
    public double VelocityFactor { get; set; } = 1.5;

    private double _minVelocityFactor = 1.2, _compleValue = 1.3;
    private const double MaxVelocityFactor = 2.5;

    [Category("Scroll Physics")]
    [Description("缓动模型的起始力度，数值越大，滚动起始速率越快，滚得越远。一般取值1~2")]
    public double MinVelocityFactor
    {
        get => _minVelocityFactor;
        set
        {
            _minVelocityFactor = value;
            _compleValue = MaxVelocityFactor - value;
        }
    }

    /// <summary>
    /// 缓动模型的速度衰减系数，数值越小，越快停下来
    /// </summary>
    [Category("Scroll Physics")]
    [Description("缓动模型的速度衰减系数，数值越小，越快停下来。取值0~1")]
    public double Friction { get; set; } = 0.92;

    /// <summary>
    /// 精确模型的插值系数，数值越大，滚动越快接近目标
    /// </summary>
    [Category("Scroll Physics")]
    [Description("精确模型的插值系数，数值越大，滚动越快接近目标。取值0~1")]
    public double LerpFactor { get; set; } = 0.5;

    private const double TargetFrameTime = 1.0 / 144.0;

    private double _velocity;
    private double _targetOffset;
    private bool _isPrecision;
    private bool _isStable = true;

    public bool IsStable => _isStable;

    public void OnScroll(double currentOffset, double delta, bool isPrecision, double minOffset, double maxOffset, int timeIntervalMs)
    {
        _isPrecision = isPrecision;
        _isStable = false;
        if (isPrecision)
        {
            _velocity = 0;
#if NET5_0_OR_GREATER
            _targetOffset = Math.Clamp(currentOffset - delta, minOffset, maxOffset);
#else
            _targetOffset = MathExtension.Clamp(currentOffset - delta, minOffset, maxOffset);
#endif
        }
        else
        {
            double vf = GetVelocityFactor(timeIntervalMs);
            Debug.WriteLine(vf);
            _velocity += -delta * vf;
        }
    }

    private double GetVelocityFactor(int ms)
    {
        //v=(5- minVelocityFactor)e^(−(ms/20) )+minVelocityFactor
        return _compleValue* Math.Exp(-(ms / 20.0)) + _minVelocityFactor;
    }

    public double Update(double currentOffset, double dt, double minOffset, double maxOffset)
    {
        if (_isStable) return currentOffset;

        double timeFactor = dt / TargetFrameTime;
        double newOffset = currentOffset;

        if (_isPrecision)
        {
            double lerp = 1.0 - Math.Pow(1.0 - LerpFactor, timeFactor);
            newOffset = currentOffset + (_targetOffset - currentOffset) * lerp;

            if (Math.Abs(_targetOffset - newOffset) < 0.5)
            {
                newOffset = _targetOffset;
                _isStable = true;
            }
        }
        else
        {
            if (Math.Abs(_velocity) < 2)
            {
                _velocity = 0;
                _isStable = true;
            }
            else
            {
                _velocity *= Math.Pow(Friction, timeFactor);
                double delta = _velocity * (timeFactor / 24.0);
#if NET5_0_OR_GREATER
                newOffset = Math.Clamp(currentOffset + delta, minOffset, maxOffset);
#else
                newOffset = MathExtension.Clamp(currentOffset + delta, minOffset, maxOffset);
#endif
            }
        }

        return newOffset;
    }
}

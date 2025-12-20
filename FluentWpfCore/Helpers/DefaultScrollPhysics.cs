using System;

namespace FluentWpfCore.Helpers
{
    public class DefaultScrollPhysics : IScrollPhysics
    {
        /// <summary>
        /// 缓动模型的叠加速度力度，数值越大，滚动起始速率越快，滚得越远
        /// </summary>
        public double VelocityFactor { get; set; } = 2.0;
        /// <summary>
        /// 缓动模型的速度衰减系数，数值越小，越快停下来
        /// </summary>
        public double Friction { get; set; } = 0.92;
        /// <summary>
        /// 精确模型的插值系数，数值越大，滚动越快接近目标
        /// </summary>
        public double LerpFactor { get; set; } = 0.5;
        
        private const double TargetFrameTime = 1.0 / 144.0;

        private double _velocity;
        private double _targetOffset;
        private bool _isPrecision;
        private bool _isStable = true;

        public bool IsStable => _isStable;

        public void OnScroll(double currentOffset, double delta, bool isPrecision, double minOffset, double maxOffset)
        {
            _isPrecision = isPrecision;
            _isStable = false;

            if (isPrecision)
            {
                _velocity = 0;
                _targetOffset = Clamp(currentOffset - delta, minOffset, maxOffset);
            }
            else
            {
                _velocity += -delta * VelocityFactor;
            }
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
                if (Math.Abs(_velocity) < 0.1)
                {
                    _velocity = 0;
                    _isStable = true;
                }
                else
                {
                    _velocity *= Math.Pow(Friction, timeFactor);
                    double delta = _velocity * (timeFactor / 24.0);
                    newOffset = Clamp(currentOffset + delta, minOffset, maxOffset);
                }
            }
            
            return newOffset;
        }

        private static double Clamp(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}

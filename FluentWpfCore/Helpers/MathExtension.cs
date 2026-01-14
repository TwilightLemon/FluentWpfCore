namespace FluentWpfCore.Helpers;

/// <summary>
/// Math extension utility class providing helper functions for mathematical operations.
/// 数学扩展工具类，提供数学操作的辅助函数。
/// </summary>
internal class MathExtension
{
    /// <summary>
    /// Clamps a value between a minimum and maximum value.
    /// 将值限制在最小值和最大值之间。
    /// </summary>
    /// <param name="value">The value to clamp. 要限制的值。</param>
    /// <param name="min">The minimum value. 最小值。</param>
    /// <param name="max">The maximum value. 最大值。</param>
    /// <returns>The clamped value. 限制后的值。</returns>
    internal static double Clamp(double value, double min, double max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }
}

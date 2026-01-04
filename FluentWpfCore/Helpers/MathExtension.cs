namespace FluentWpfCore.Helpers;

/// <summary>
/// 数学扩展辅助类，提供常用的数学计算方法
/// Math extension helper class providing common mathematical calculation methods
/// </summary>
internal class MathExtension
{
    /// <summary>
    /// 将值限制在指定范围内
    /// Clamps a value within the specified range
    /// </summary>
    /// <param name="value">待限制的值 / Value to clamp</param>
    /// <param name="min">最小值 / Minimum value</param>
    /// <param name="max">最大值 / Maximum value</param>
    /// <returns>限制后的值 / Clamped value</returns>
    internal static double Clamp(double value, double min, double max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }
}

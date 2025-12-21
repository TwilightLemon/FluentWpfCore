namespace FluentWpfCore.Helpers;

internal class MathExtension
{
#if !NET5_0_OR_GREATER
    //For .net 4.5 compatibility
    internal static double Clamp(double value, double min, double max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }
#endif
}

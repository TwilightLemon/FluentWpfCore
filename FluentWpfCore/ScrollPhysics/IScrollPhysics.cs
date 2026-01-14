using System.Reflection;

namespace FluentWpfCore.ScrollPhysics
{
    /// <summary>
    /// Interface for scroll physics models that control smooth scrolling animation behavior.
    /// 滚动物理模型接口，控制平滑滚动动画行为。
    /// </summary>
    /// <remarks>
    /// Implement this interface to create custom scrolling behaviors with different
    /// acceleration, deceleration, and friction characteristics.
    /// 实现此接口以创建具有不同加速、减速和摩擦特性的自定义滚动行为。
    /// </remarks>
    public interface IScrollPhysics
    {
        /// <summary>
        /// Called when a scroll input is received.
        /// 接收到滚动输入时调用。
        /// </summary>
        /// <param name="delta">The scroll amount. 滚动量。</param>
        void OnScroll(double delta);
        
        /// <summary>
        /// Updates the scroll offset based on the current state and elapsed time.
        /// 根据当前状态和经过的时间更新滚动偏移量。
        /// </summary>
        /// <param name="currentOffset">The current scroll offset. 当前滚动偏移量。</param>
        /// <param name="dt">Delta time since last update in seconds. 自上次更新以来的增量时间（秒）。</param>
        /// <returns>The new scroll offset. 新的滚动偏移量。</returns>
        double Update(double currentOffset, double dt);
        
        /// <summary>
        /// Gets a value indicating whether the scrolling animation has stabilized.
        /// 获取一个值，指示滚动动画是否已稳定。
        /// </summary>
        bool IsStable { get; }

        /// <summary>
        /// Gets or sets a value indicating whether precise mode is enabled (e.g., for touch input).
        /// 获取或设置一个值，指示是否启用精确模式（例如，用于触摸输入）。
        /// </summary>
        bool IsPreciseMode { get; set; }
    }

    internal static class IScrollPhysicsExtension
    {
        internal static IScrollPhysics Clone(this IScrollPhysics source)
        {
            Type type = source.GetType();
            if (Activator.CreateInstance(type) is not IScrollPhysics clone)
            {
                throw new InvalidOperationException($"Unable to create physics instance of type {type.FullName}.");
            }

            foreach (PropertyInfo property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (!property.CanRead || !property.CanWrite) continue;
                if (property.GetIndexParameters().Length > 0) continue;

                object? value = property.GetValue(source, null);
                property.SetValue(clone, value, null);
            }

            return clone;
        }
    }
}

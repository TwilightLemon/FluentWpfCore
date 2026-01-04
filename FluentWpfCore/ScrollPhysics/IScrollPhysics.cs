using System.Reflection;

namespace FluentWpfCore.ScrollPhysics
{
    /// <summary>
    /// 滚动物理模型接口，用于定义滚动动画的物理行为
    /// Scroll physics interface for defining the physical behavior of scroll animations
    /// </summary>
    public interface IScrollPhysics
    {
        /// <summary>
        /// 处理滚动输入事件
        /// Handles scroll input events
        /// </summary>
        /// <param name="delta">滚动量（正值向下/右，负值向上/左）/ Scroll delta (positive for down/right, negative for up/left)</param>
        void OnScroll(double delta);

        /// <summary>
        /// 更新滚动位置（每帧调用）
        /// Updates scroll position (called every frame)
        /// </summary>
        /// <param name="currentOffset">当前偏移量 / Current offset</param>
        /// <param name="dt">距离上一帧的时间增量（秒）/ Time delta since last frame (seconds)</param>
        /// <returns>新的偏移量 / New offset</returns>
        double Update(double currentOffset, double dt);

        /// <summary>
        /// 获取滚动是否已稳定（动画结束）
        /// Gets whether scrolling is stable (animation ended)
        /// </summary>
        bool IsStable { get; }

        /// <summary>
        /// 获取或设置是否为精确模式（触摸、触控笔输入）
        /// Gets or sets whether in precise mode (touch, stylus input)
        /// </summary>
        bool IsPreciseMode { get; set; }
    }

    /// <summary>
    /// 滚动物理扩展方法
    /// Scroll physics extension methods
    /// </summary>
    internal static class IScrollPhysicsExtension
    {
        /// <summary>
        /// 克隆滚动物理对象（深拷贝所有公共属性）
        /// Clones a scroll physics object (deep copy of all public properties)
        /// </summary>
        /// <param name="source">源对象 / Source object</param>
        /// <returns>克隆的对象 / Cloned object</returns>
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

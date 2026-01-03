using System.Reflection;

namespace FluentWpfCore.ScrollPhysics
{
    public interface IScrollPhysics
    {
        void OnScroll(double delta);
        double Update(double currentOffset, double dt);
        bool IsStable { get; }
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

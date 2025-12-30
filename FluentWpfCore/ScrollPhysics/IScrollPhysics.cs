namespace FluentWpfCore.ScrollPhysics
{
    public interface IScrollPhysics
    {
        void OnScroll(double delta);
        double Update(double currentOffset, double dt);
        bool IsStable { get; }
    }
}

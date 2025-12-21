namespace FluentWpfCore.Helpers
{
    public interface IScrollPhysics
    {
        void OnScroll(double currentOffset, double delta, bool isPrecision, double minOffset, double maxOffset, int timeIntervalMs);
        double Update(double currentOffset, double dt, double minOffset, double maxOffset);
        bool IsStable { get; }
    }
}

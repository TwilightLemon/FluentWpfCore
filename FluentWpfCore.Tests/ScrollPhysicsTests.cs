using FluentWpfCore.ScrollPhysics;
using Xunit;

namespace FluentWpfCore.Tests;

public class ScrollPhysicsTests
{
    private const double FrameTime = 1.0 / 144.0;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DefaultPhysicsConvergesAndPreservesTheRequestedDistance(bool preciseMode)
    {
        AssertConvergesAndPreservesDistance(new DefaultScrollPhysics(), preciseMode);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExponentialPhysicsConvergesAndPreservesTheRequestedDistance(bool preciseMode)
    {
        AssertConvergesAndPreservesDistance(new ExponentialScrollPhysics(), preciseMode);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DefaultPhysicsAcceptsMoreInputWhileAnimating(bool preciseMode)
    {
        AssertAcceptsMoreInputWhileAnimating(new DefaultScrollPhysics(), preciseMode);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExponentialPhysicsAcceptsMoreInputWhileAnimating(bool preciseMode)
    {
        AssertAcceptsMoreInputWhileAnimating(new ExponentialScrollPhysics(), preciseMode);
    }

    [Fact]
    public void DefaultPhysicsResetClearsPendingMotion()
    {
        AssertResetClearsPendingMotion(new DefaultScrollPhysics());
    }

    [Fact]
    public void ExponentialPhysicsResetClearsPendingMotion()
    {
        AssertResetClearsPendingMotion(new ExponentialScrollPhysics());
    }

    private static void AssertConvergesAndPreservesDistance(IScrollPhysics physics, bool preciseMode)
    {
        const double initialOffset = 250.0;
        const double requestedDelta = 123.25;

        physics.IsPreciseMode = preciseMode;
        physics.OnScroll(requestedDelta);

        double finalOffset = RunUntilStable(physics, initialOffset);

        Assert.Equal(initialOffset - requestedDelta, finalOffset, precision: 10);
    }

    private static void AssertAcceptsMoreInputWhileAnimating(IScrollPhysics physics, bool preciseMode)
    {
        const double initialOffset = 250.0;
        const double firstDelta = 120.0;
        const double secondDelta = -40.5;

        physics.IsPreciseMode = preciseMode;
        physics.OnScroll(firstDelta);

        double offset = initialOffset;
        for (int i = 0; i < 5; i++)
        {
            offset = physics.Update(offset, FrameTime);
        }

        physics.OnScroll(secondDelta);
        double finalOffset = RunUntilStable(physics, offset);

        Assert.Equal(initialOffset - firstDelta - secondDelta, finalOffset, precision: 10);
    }

    private static void AssertResetClearsPendingMotion(IScrollPhysics physics)
    {
        const double initialOffset = 250.0;

        physics.IsPreciseMode = true;
        physics.OnScroll(120.0);
        double offset = physics.Update(initialOffset, FrameTime);

        physics.Reset();

        Assert.True(physics.IsStable);
        Assert.Equal(offset, physics.Update(offset, FrameTime));
    }

    private static double RunUntilStable(IScrollPhysics physics, double offset)
    {
        const int maximumFrameCount = 10_000;

        for (int frame = 0; frame < maximumFrameCount && !physics.IsStable; frame++)
        {
            offset = physics.Update(offset, FrameTime);
        }

        Assert.True(physics.IsStable, "The physics model did not converge within the frame limit.");
        return offset;
    }
}

using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using Xunit;

namespace FluentWpfCore.Tests;

public class DependencyPropertyBehaviorTests
{
    [Fact]
    public void SetCurrentValueUpdatesValueWithoutRemovingBinding()
    {
        Exception? threadException = null;
        var thread = new Thread(() =>
        {
            try
            {
                var source = new ScrollBar { Maximum = 100, Value = 10 };
                var target = new ScrollBar { Maximum = 100 };
                BindingOperations.SetBinding(
                    target,
                    RangeBase.ValueProperty,
                    new Binding(nameof(RangeBase.Value))
                    {
                        Source = source,
                        Mode = BindingMode.OneWay
                    });

                target.SetCurrentValue(RangeBase.ValueProperty, 42.0);

                Assert.True(BindingOperations.IsDataBound(target, RangeBase.ValueProperty));
                Assert.Equal(42.0, target.Value);

                source.Value = 17.0;
                Assert.Equal(17.0, target.Value);
            }
            catch (Exception exception)
            {
                threadException = exception;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (threadException != null)
        {
            ExceptionDispatchInfo.Capture(threadException).Throw();
        }
    }
}

using Atria.Feed.Ingestor.ChainClients;
using FluentAssertions;

namespace Atria.Feed.Ingestor.Tests.ChainClients;

public class ReorgContextTests
{
    [Fact]
    public void IsActive_DefaultsFalse()
    {
        ReorgContext.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Activate_SetsIsActiveTrue()
    {
        using var scope = ReorgContext.Activate();
        ReorgContext.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Dispose_ResetsIsActiveFalse()
    {
        var scope = ReorgContext.Activate();
        scope.Dispose();
        ReorgContext.IsActive.Should().BeFalse();
    }

    [Fact]
    public void NestedActivate_OuterDisposeClearsContext()
    {
        using var outer = ReorgContext.Activate();

        using (var inner = ReorgContext.Activate())
        {
            ReorgContext.IsActive.Should().BeTrue();
        }

        ReorgContext.IsActive.Should().BeTrue("inner NullScope dispose should not clear context");
    }

    [Fact]
    public void DoubleDispose_DoesNotResetActiveContext()
    {
        using var active = ReorgContext.Activate();

        var scope = ReorgContext.Activate();
        scope.Dispose();
        scope.Dispose();

        ReorgContext.IsActive.Should().BeTrue("double dispose on outer scope should be safe");
    }

    [Fact]
    public async Task AsyncLocal_IsolatedBetweenTasks()
    {
        using var barrier = new ManualResetEventSlim(false);
        var task1Active = false;
        var task2Active = false;

        var task1 = Task.Run(() =>
        {
            using var scope = ReorgContext.Activate();
            barrier.Set();
            task1Active = ReorgContext.IsActive;
        });

        var task2 = Task.Run(() =>
        {
            barrier.Wait();
            task2Active = ReorgContext.IsActive;
        });

        await Task.WhenAll(task1, task2);

        task1Active.Should().BeTrue();
        task2Active.Should().BeFalse();
    }
}

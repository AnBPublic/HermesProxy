using HermesProxy.World;
using Xunit;

namespace HermesProxy.Tests.World;

public class ProxyOrderingTests
{
    [Fact]
    public void DelayedPackets_DrainInArrivalOrder()
    {
        var queue = new OrderedPacketQueue<int>();

        queue.Enqueue(1);
        queue.Enqueue(2);
        queue.Enqueue(3);

        Assert.Equal(new[] { 1, 2, 3 }, queue.Drain());
    }

    [Fact]
    public void LootTargetState_IsUpdatedBeforeForwardCallbackRuns()
    {
        var state = new LootTargetState();
        var target = new WowGuid64(0x1234);
        WowGuid64 observed = default;

        state.BeginRequest(target, () => observed = state.Current);

        Assert.Equal(target, observed);
        Assert.Equal(target, state.Current);
    }

    [Theory]
    [InlineData(true, false, true)]
    [InlineData(false, false, true)]
    [InlineData(true, true, true)]
    public void TcpNoDelayPolicy_RespectsForcedPolicy(bool forceNoDelay, bool clientRequestedNagle, bool expected)
    {
        Assert.Equal(expected, TcpNoDelayPolicy.Resolve(forceNoDelay, clientRequestedNagle));
    }
}

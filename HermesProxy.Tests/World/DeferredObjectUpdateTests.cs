using System;
using HermesProxy.World;
using HermesProxy.World.Client;
using Xunit;

namespace HermesProxy.Tests.World;

public sealed class DeferredObjectUpdateTests
{
    [Fact]
    public void ItemQueryWaitExpiresAtTheConfiguredDeadline()
    {
        var now = DateTimeOffset.UtcNow;

        Assert.False(DeferredObjectUpdatePolicy.HasExpired(now - DeferredObjectUpdatePolicy.Timeout + TimeSpan.FromMilliseconds(1), now));
        Assert.True(DeferredObjectUpdatePolicy.HasExpired(now - DeferredObjectUpdatePolicy.Timeout - TimeSpan.FromMilliseconds(1), now));
    }
}

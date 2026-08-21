using System;
using HermesProxy.World;
using HermesProxy.World.Objects;
using Xunit;

namespace HermesProxy.Tests.ProtocolReplay;

public sealed class ItemQueryCoordinatorReplayTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 21, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CacheHitAndInvalidationAvoidAndThenRestoreABackendRequest()
    {
        var coordinator = new ItemQueryCoordinator(2, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(2));
        var initial = new ItemQueryWaiter(1, [101], Now);
        Assert.Equal([101u], coordinator.Schedule(initial, Now).BackendRequests);
        coordinator.Resolve(101, ItemQueryResolution.Found, Now);

        var warm = new ItemQueryWaiter(2, [101], Now);
        Assert.Empty(coordinator.Schedule(warm, Now).BackendRequests);
        coordinator.Invalidate(101);
        Assert.Equal([101u], coordinator.Schedule(new ItemQueryWaiter(3, [101], Now), Now).BackendRequests);
    }

    [Fact]
    public void TemplateCacheIsBoundedAndUsesRecentAccessForEviction()
    {
        var cache = new BoundedItemTemplateCache(2);
        cache.Store(111, new ItemTemplate());
        cache.Store(112, new ItemTemplate());
        Assert.True(cache.TryGetValue(111, out _));
        cache.Store(113, new ItemTemplate());

        Assert.False(cache.TryGetValue(112, out _));
        Assert.True(cache.TryGetValue(111, out _));
        cache.Invalidate(111);
        Assert.False(cache.TryGetValue(111, out _));
    }

    [Fact]
    public void DuplicateAndOutOfOrderResponsesReleaseOnlyTheirOwnWaiters()
    {
        var coordinator = new ItemQueryCoordinator(4, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(2));
        var first = new ItemQueryWaiter(1, [201], Now);
        var duplicate = new ItemQueryWaiter(2, [201], Now);
        var other = new ItemQueryWaiter(3, [202], Now);

        Assert.Equal([201u], coordinator.Schedule(first, Now).BackendRequests);
        Assert.Empty(coordinator.Schedule(duplicate, Now).BackendRequests);
        Assert.Equal([202u], coordinator.Schedule(other, Now).BackendRequests);
        Assert.Equal([other], coordinator.Resolve(202, ItemQueryResolution.Found, Now));
        Assert.Equal([first, duplicate], coordinator.Resolve(201, ItemQueryResolution.Found, Now));
    }

    [Fact]
    public void MissingTemplateTimeoutReconnectAndLaterDependenciesAreIndependent()
    {
        var coordinator = new ItemQueryCoordinator(2, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(2));
        var ordered = new ItemQueryWaiter(1, [301], Now);
        coordinator.Schedule(ordered, Now);
        Assert.Equal([302u], coordinator.AddDependencies(ordered, [302], Now));
        Assert.Empty(coordinator.Resolve(301, ItemQueryResolution.Missing, Now));
        Assert.Equal([ordered], coordinator.Resolve(302, ItemQueryResolution.Found, Now));

        var negative = new ItemQueryWaiter(2, [301], Now);
        Assert.Empty(coordinator.Schedule(negative, Now).BackendRequests);
        Assert.Equal([301u], coordinator.Schedule(new ItemQueryWaiter(3, [301], Now + TimeSpan.FromSeconds(10)), Now + TimeSpan.FromSeconds(10)).BackendRequests);

        var timedOut = new ItemQueryWaiter(4, [401], Now);
        coordinator.Schedule(timedOut, Now);
        Assert.Equal([timedOut], coordinator.Expire(Now + TimeSpan.FromSeconds(2)));
        var interrupted = new ItemQueryWaiter(5, [402], Now);
        coordinator.Schedule(interrupted, Now);
        Assert.Contains(interrupted, coordinator.ResetForReconnect());
        Assert.Equal([402u], coordinator.Schedule(new ItemQueryWaiter(6, [402], Now), Now).BackendRequests);
    }
}

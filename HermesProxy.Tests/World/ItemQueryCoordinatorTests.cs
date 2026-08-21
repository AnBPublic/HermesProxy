using System;
using HermesProxy.World;
using Xunit;

namespace HermesProxy.Tests.World;

public sealed class ItemQueryCoordinatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 21, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CacheHitIsReadyWithoutABackendRequest()
    {
        var coordinator = new ItemQueryCoordinator(2, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(2));
        var first = new ItemQueryWaiter(1, [101], Now);
        Assert.Equal([101u], coordinator.Schedule(first, Now).BackendRequests);
        coordinator.Resolve(101, ItemQueryResolution.Found, Now);

        var warm = new ItemQueryWaiter(2, [101], Now);
        var result = coordinator.Schedule(warm, Now);
        Assert.Empty(result.BackendRequests);
        Assert.Equal([warm], result.ReadyWaiters);
    }

    [Fact]
    public void DuplicateRequestsCoalesceAndOutOfOrderResponsesReleaseOnlyTheirWaiters()
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
    public void LaterUpdateForTheSameGuidCanAddItsOwnDependencyWithoutReorderingTheWaiter()
    {
        var coordinator = new ItemQueryCoordinator(4, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(2));
        var waiter = new ItemQueryWaiter(1, [211], Now);
        coordinator.Schedule(waiter, Now);

        Assert.Equal([212u], coordinator.AddDependencies(waiter, [212], Now));
        Assert.Empty(coordinator.Resolve(211, ItemQueryResolution.Found, Now));
        Assert.Equal([waiter], coordinator.Resolve(212, ItemQueryResolution.Found, Now));
    }

    [Fact]
    public void MissingTemplateExpiresFromNegativeCacheAndCanBeInvalidated()
    {
        var coordinator = new ItemQueryCoordinator(2, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(2));
        var initial = new ItemQueryWaiter(1, [301], Now);
        coordinator.Schedule(initial, Now);
        coordinator.Resolve(301, ItemQueryResolution.Missing, Now);

        Assert.Empty(coordinator.Schedule(new ItemQueryWaiter(2, [301], Now + TimeSpan.FromSeconds(9)), Now + TimeSpan.FromSeconds(9)).BackendRequests);
        Assert.Equal([301u], coordinator.Schedule(new ItemQueryWaiter(3, [301], Now + TimeSpan.FromSeconds(10)), Now + TimeSpan.FromSeconds(10)).BackendRequests);

        coordinator.Resolve(301, ItemQueryResolution.Found, Now + TimeSpan.FromSeconds(10));
        coordinator.Invalidate(301);
        Assert.Equal([301u], coordinator.Schedule(new ItemQueryWaiter(4, [301], Now + TimeSpan.FromSeconds(11)), Now + TimeSpan.FromSeconds(11)).BackendRequests);
    }

    [Fact]
    public void TimeoutAndReconnectReleaseWaitersWithoutBlockingNewRequests()
    {
        var coordinator = new ItemQueryCoordinator(2, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(2));
        var timedOut = new ItemQueryWaiter(1, [401], Now);
        coordinator.Schedule(timedOut, Now);
        Assert.Equal([timedOut], coordinator.Expire(Now + TimeSpan.FromSeconds(2)));

        var interrupted = new ItemQueryWaiter(2, [402], Now);
        coordinator.Schedule(interrupted, Now);
        Assert.Equal([interrupted], coordinator.ResetForReconnect());
        Assert.Equal([402u], coordinator.Schedule(new ItemQueryWaiter(3, [402], Now), Now).BackendRequests);
    }

    [Fact]
    public void CacheEvictsLeastRecentlyUsedEntry()
    {
        var coordinator = new ItemQueryCoordinator(2, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(2));
        SeedFound(coordinator, 501);
        SeedFound(coordinator, 502);
        SeedFound(coordinator, 503);

        Assert.Equal([501u], coordinator.Schedule(new ItemQueryWaiter(4, [501], Now), Now).BackendRequests);
    }

    private static void SeedFound(ItemQueryCoordinator coordinator, uint itemId)
    {
        var waiter = new ItemQueryWaiter(itemId, [itemId], Now);
        coordinator.Schedule(waiter, Now);
        coordinator.Resolve(itemId, ItemQueryResolution.Found, Now);
    }
}

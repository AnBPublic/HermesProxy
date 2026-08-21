using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Framework.Logging;

namespace HermesProxy.World;

public enum ItemQueryResolution
{
    Found,
    Missing
}

public sealed class ItemQueryWaiter
{
    private readonly HashSet<uint> _itemIds;
    private readonly HashSet<uint> _pendingItemIds;

    public ItemQueryWaiter(long sequence, IEnumerable<uint> itemIds, DateTimeOffset enqueuedAtUtc)
    {
        Sequence = sequence;
        _itemIds = new HashSet<uint>(itemIds);
        _pendingItemIds = new HashSet<uint>(_itemIds);
        EnqueuedAtUtc = enqueuedAtUtc;
    }

    public long Sequence { get; }
    public IReadOnlyCollection<uint> ItemIds => _itemIds;
    public DateTimeOffset EnqueuedAtUtc { get; }
    public IReadOnlyCollection<uint> PendingItemIds => _pendingItemIds;

    internal bool Resolve(uint itemId) => _pendingItemIds.Remove(itemId) && _pendingItemIds.Count == 0;
    internal bool AddDependency(uint itemId) => _itemIds.Add(itemId) && _pendingItemIds.Add(itemId);
    internal bool IsExpired(DateTimeOffset now, TimeSpan timeout) => now - EnqueuedAtUtc >= timeout;
}

public readonly record struct ItemQueryScheduleResult(
    IReadOnlyList<uint> BackendRequests,
    IReadOnlyList<ItemQueryWaiter> ReadyWaiters);

public sealed class ItemQueryCoordinator
{
    private readonly int _maxCacheEntries;
    private readonly TimeSpan _negativeCacheTtl;
    private readonly TimeSpan _waiterTimeout;
    private readonly object _lock = new();
    private readonly Dictionary<uint, CacheEntry> _cache = [];
    private readonly LinkedList<uint> _cacheRecency = [];
    private readonly Dictionary<uint, List<ItemQueryWaiter>> _waitersByItem = [];
    private readonly HashSet<ItemQueryWaiter> _pendingWaiters = [];

    public ItemQueryCoordinator(int maxCacheEntries, TimeSpan negativeCacheTtl, TimeSpan waiterTimeout)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxCacheEntries, 1);
        if (negativeCacheTtl <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(negativeCacheTtl));
        if (waiterTimeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(waiterTimeout));

        _maxCacheEntries = maxCacheEntries;
        _negativeCacheTtl = negativeCacheTtl;
        _waiterTimeout = waiterTimeout;
    }

    public ItemQueryScheduleResult Schedule(ItemQueryWaiter waiter, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(waiter);

        lock (_lock)
        {
            List<uint> backendRequests = [];
            foreach (uint itemId in waiter.ItemIds)
            {
                if (TryUseCache(itemId, now))
                {
                    waiter.Resolve(itemId);
                    continue;
                }

                if (!_waitersByItem.TryGetValue(itemId, out var waiters))
                {
                    waiters = [];
                    _waitersByItem.Add(itemId, waiters);
                    backendRequests.Add(itemId);
                }
                waiters.Add(waiter);
            }

            if (waiter.PendingItemIds.Count == 0)
                return new ItemQueryScheduleResult(backendRequests, [waiter]);

            _pendingWaiters.Add(waiter);
            return new ItemQueryScheduleResult(backendRequests, []);
        }
    }

    public bool Request(uint itemId, DateTimeOffset now)
    {
        lock (_lock)
        {
            if (TryUseCache(itemId, now) || _waitersByItem.ContainsKey(itemId))
                return false;

            _waitersByItem.Add(itemId, []);
            return true;
        }
    }

    public IReadOnlyList<uint> AddDependencies(ItemQueryWaiter waiter, IEnumerable<uint> itemIds, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(waiter);
        ArgumentNullException.ThrowIfNull(itemIds);

        lock (_lock)
        {
            List<uint> backendRequests = [];
            foreach (uint itemId in itemIds.Distinct())
            {
                if (!waiter.AddDependency(itemId))
                    continue;

                if (TryUseCache(itemId, now))
                {
                    waiter.Resolve(itemId);
                    continue;
                }

                if (!_waitersByItem.TryGetValue(itemId, out var waiters))
                {
                    waiters = [];
                    _waitersByItem.Add(itemId, waiters);
                    backendRequests.Add(itemId);
                }

                waiters.Add(waiter);
            }

            if (waiter.PendingItemIds.Count != 0)
                _pendingWaiters.Add(waiter);

            return backendRequests;
        }
    }

    public IReadOnlyList<ItemQueryWaiter> Resolve(uint itemId, ItemQueryResolution resolution, DateTimeOffset now)
    {
        lock (_lock)
        {
            StoreCacheEntry(itemId, resolution, now);
            if (!_waitersByItem.Remove(itemId, out var waiters))
                return [];

            List<ItemQueryWaiter> ready = [];
            foreach (var waiter in waiters)
            {
                if (waiter.Resolve(itemId))
                {
                    _pendingWaiters.Remove(waiter);
                    ready.Add(waiter);
                }
            }

            return ready.OrderBy(waiter => waiter.Sequence).ToArray();
        }
    }

    public IReadOnlyList<ItemQueryWaiter> Expire(DateTimeOffset now)
    {
        lock (_lock)
        {
            var expired = _pendingWaiters.Where(waiter => waiter.IsExpired(now, _waiterTimeout))
                .OrderBy(waiter => waiter.Sequence)
                .ToArray();
            foreach (var waiter in expired)
            {
                _pendingWaiters.Remove(waiter);
                foreach (uint itemId in waiter.PendingItemIds.ToArray())
                {
                    if (_waitersByItem.TryGetValue(itemId, out var waiters))
                    {
                        waiters.Remove(waiter);
                        if (waiters.Count == 0)
                            _waitersByItem.Remove(itemId);
                    }
                }
            }

            return expired;
        }
    }

    public IReadOnlyList<ItemQueryWaiter> ResetForReconnect()
    {
        lock (_lock)
        {
            var interrupted = _pendingWaiters.OrderBy(waiter => waiter.Sequence).ToArray();
            _pendingWaiters.Clear();
            _waitersByItem.Clear();
            return interrupted;
        }
    }

    public void Invalidate(uint itemId)
    {
        lock (_lock)
        {
            if (_cache.Remove(itemId, out var entry))
                _cacheRecency.Remove(entry.RecencyNode);
        }
    }

    private bool TryUseCache(uint itemId, DateTimeOffset now)
    {
        if (!_cache.TryGetValue(itemId, out var entry))
        {
            Log.Print(LogType.Network, $"[ItemQuery] cache-miss item={itemId}");
            return false;
        }
        if (entry.NegativeExpiresAtUtc is DateTimeOffset expiresAtUtc && expiresAtUtc <= now)
        {
            _cache.Remove(itemId);
            _cacheRecency.Remove(entry.RecencyNode);
            Log.Print(LogType.Network, $"[ItemQuery] negative-cache-expired item={itemId}");
            return false;
        }

        _cacheRecency.Remove(entry.RecencyNode);
        _cacheRecency.AddLast(entry.RecencyNode);
        Log.Print(LogType.Network, $"[ItemQuery] {(entry.NegativeExpiresAtUtc.HasValue ? "negative-cache-hit" : "cache-hit")} item={itemId}");
        return true;
    }

    private void StoreCacheEntry(uint itemId, ItemQueryResolution resolution, DateTimeOffset now)
    {
        Invalidate(itemId);
        var recencyNode = _cacheRecency.AddLast(itemId);
        _cache.Add(itemId, new CacheEntry(
            recencyNode,
            resolution == ItemQueryResolution.Missing ? now + _negativeCacheTtl : null));

        while (_cache.Count > _maxCacheEntries)
        {
            var leastRecent = _cacheRecency.First!;
            _cacheRecency.RemoveFirst();
            _cache.Remove(leastRecent.Value);
        }
    }

    private sealed record CacheEntry(LinkedListNode<uint> RecencyNode, DateTimeOffset? NegativeExpiresAtUtc);
}

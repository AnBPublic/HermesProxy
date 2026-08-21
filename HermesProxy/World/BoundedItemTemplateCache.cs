using System;
using System.Collections.Generic;
using System.Threading;
using HermesProxy.World.Objects;

namespace HermesProxy.World;

public sealed class BoundedItemTemplateCache
{
    private readonly int _capacity;
    private readonly Lock _lock = new();
    private readonly Dictionary<uint, Entry> _entries = [];
    private readonly LinkedList<uint> _recency = [];

    public BoundedItemTemplateCache(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);
        _capacity = capacity;
    }

    public bool ContainsKey(uint itemId) => TryGetValue(itemId, out _);

    public bool TryGetValue(uint itemId, out ItemTemplate template)
    {
        lock (_lock)
        {
            if (!_entries.TryGetValue(itemId, out var entry))
            {
                template = null!;
                return false;
            }

            _recency.Remove(entry.Node);
            _recency.AddLast(entry.Node);
            template = entry.Template;
            return true;
        }
    }

    public void Store(uint itemId, ItemTemplate template)
    {
        lock (_lock)
        {
            if (_entries.Remove(itemId, out var existing))
                _recency.Remove(existing.Node);

            var node = _recency.AddLast(itemId);
            _entries.Add(itemId, new Entry(template, node));
            if (_entries.Count <= _capacity)
                return;

            var oldest = _recency.First!;
            _recency.RemoveFirst();
            _entries.Remove(oldest.Value);
        }
    }

    public void Invalidate(uint itemId)
    {
        lock (_lock)
        {
            if (_entries.Remove(itemId, out var entry))
                _recency.Remove(entry.Node);
        }
    }

    private sealed record Entry(ItemTemplate Template, LinkedListNode<uint> Node);
}

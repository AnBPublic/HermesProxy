using System.Collections.Generic;
using System.Threading;

namespace HermesProxy.World;

internal sealed class OrderedPacketQueue<T>
{
    private readonly Queue<T> _items = new();
    private readonly Lock _lock = new();

    public int Count
    {
        get
        {
            lock (_lock)
                return _items.Count;
        }
    }

    public void Enqueue(T item)
    {
        lock (_lock)
            _items.Enqueue(item);
    }

    public T[] Drain()
    {
        lock (_lock)
        {
            var items = _items.ToArray();
            _items.Clear();
            return items;
        }
    }
}

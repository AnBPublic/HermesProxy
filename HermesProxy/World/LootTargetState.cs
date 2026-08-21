using System;
using System.Threading;

namespace HermesProxy.World;

public sealed class LootTargetState
{
    private readonly Lock _lock = new();
    private WowGuid64 _current;

    public WowGuid64 Current
    {
        get
        {
            lock (_lock)
                return _current;
        }
    }

    public void Set(WowGuid64 target)
    {
        lock (_lock)
            _current = target;
    }

    public void BeginRequest(WowGuid64 target, Action forward)
    {
        ArgumentNullException.ThrowIfNull(forward);
        lock (_lock)
        {
            _current = target;
            forward();
        }
    }
}

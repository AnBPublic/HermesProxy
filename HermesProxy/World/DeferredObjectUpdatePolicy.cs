using System;

namespace HermesProxy.World;

public static class DeferredObjectUpdatePolicy
{
    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(2);

    public static bool HasExpired(DateTimeOffset enqueuedAtUtc, DateTimeOffset nowUtc)
        => nowUtc - enqueuedAtUtc >= Timeout;
}

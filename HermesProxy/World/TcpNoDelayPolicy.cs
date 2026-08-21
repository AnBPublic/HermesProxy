namespace HermesProxy.World;

internal static class TcpNoDelayPolicy
{
    public static bool Resolve(bool forceNoDelay, bool clientRequestedNagle)
    {
        return forceNoDelay || !clientRequestedNagle;
    }
}

namespace HermesProxy.World;

public enum SessionLifecycleState
{
    Connecting,
    Authenticating,
    RealmReady,
    InstanceReady,
    InWorld,
    LoggingOut,
    ClientExited,
    Faulted
}

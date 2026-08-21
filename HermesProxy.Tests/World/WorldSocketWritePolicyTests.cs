using Framework.Constants;
using HermesProxy.World.Server;
using Xunit;

namespace HermesProxy.Tests.World;

public sealed class WorldSocketWritePolicyTests
{
    [Fact]
    public void RealmHandshakeUsesSynchronousWritesWhileInstanceKeepsQueuedWrites()
    {
        Assert.True(WorldSocket.ShouldUseSynchronousHandshakeWrites(ConnectionType.Realm));
        Assert.False(WorldSocket.ShouldUseSynchronousHandshakeWrites(ConnectionType.Instance));
    }
}

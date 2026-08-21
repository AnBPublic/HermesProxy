using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using HermesProxy.World;
using HermesProxy.World.Client;
using HermesProxy.World.Enums;
using Xunit;

namespace HermesProxy.Tests.World;

public sealed class SpellCastLifecycleTests
{
    [Fact]
    public void OwnSpellFailure_RemovesStartedCastFromPendingQueue()
    {
        var gameState = CreateGameState();
        var ownGuid = new WowGuid128(0x1234, 0x3000000000000000);
        gameState.CurrentPlayerGuid = ownGuid;
        var pendingCast = new ClientCastRequest { SpellId = 123, HasStarted = true };
        gameState.PendingNormalCasts.Enqueue(pendingCast);

        var found = gameState.TryDequeueOwnPendingNormalCast(ownGuid, 123, out var dequeuedCast);

        Assert.True(found);
        Assert.Same(pendingCast, dequeuedCast);
        Assert.False(gameState.HasStartedNormalCast());
    }

    [Fact]
    public void OtherUnitSpellFailure_DoesNotRemoveOwnPendingCast()
    {
        var gameState = CreateGameState();
        var ownGuid = new WowGuid128(0x1234, 0x3000000000000000);
        var otherGuid = new WowGuid128(0x5678, 0x3000000000000000);
        gameState.CurrentPlayerGuid = ownGuid;
        var pendingCast = new ClientCastRequest { SpellId = 123, HasStarted = true };
        gameState.PendingNormalCasts.Enqueue(pendingCast);

        var found = gameState.TryDequeueOwnPendingNormalCast(otherGuid, 123, out var dequeuedCast);

        Assert.False(found);
        Assert.Null(dequeuedCast);
        Assert.True(gameState.HasStartedNormalCast());
        Assert.True(gameState.PendingNormalCasts.TryPeek(out var remainingCast));
        Assert.Same(pendingCast, remainingCast);
    }

    [Fact]
    public void OwnAndOtherSpellFailures_UseTheTerminalFailureHandler()
    {
        var method = typeof(WorldClient).GetMethod("HandleSpellFailedOther", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);

        var opcodes = method!.GetCustomAttributes<PacketHandlerAttribute>()
            .Select(attribute => attribute.Opcode)
            .ToHashSet();

        Assert.Contains(Opcode.SMSG_SPELL_FAILURE, opcodes);
        Assert.Contains(Opcode.SMSG_SPELL_FAILED_OTHER, opcodes);
    }

    private static GameSessionData CreateGameState()
    {
        var gameState = (GameSessionData)RuntimeHelpers.GetUninitializedObject(typeof(GameSessionData));
        gameState.PendingNormalCasts = new ConcurrentQueue<ClientCastRequest>();
        return gameState;
    }
}

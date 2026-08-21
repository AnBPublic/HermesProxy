using System.Runtime.CompilerServices;
using HermesProxy.World.Client;
using HermesProxy.World;
using HermesProxy.World.Enums;
using HermesProxy.World.Server.Packets;
using Xunit;

namespace HermesProxy.Tests.World;

public class LootTranslationTests
{
    [Fact]
    public void ParseLootList_ReadsAbsentOptionalLooters()
    {
        var state = CreateGameState();
        var owner = new WowGuid64(HighGuidTypeLegacy.Creature, 1234, 42);
        var payload = new WorldPacket();
        payload.WriteGuid(owner);
        payload.WriteUInt8(0);
        payload.WriteUInt8(0);

        var translated = WorldClient.ParseLootList(new WorldPacket(1, payload.GetData()), state);

        Assert.Equal(owner.To128(state), translated.Owner);
        Assert.Equal(owner.ToLootGuid(), translated.LootObj);
        Assert.True(translated.Master.IsEmpty());
        Assert.True(translated.RoundRobinWinner.IsEmpty());
    }

    [Fact]
    public void ParseLootList_ReadsMasterAndRoundRobinWinnerPackedGuids()
    {
        var state = CreateGameState();
        var owner = new WowGuid64(HighGuidTypeLegacy.Creature, 5678, 99);
        var master = new WowGuid64(HighGuidTypeLegacy.Player, 7);
        var winner = new WowGuid64(HighGuidTypeLegacy.Player, 8);
        var payload = new WorldPacket();
        payload.WriteGuid(owner);
        payload.WritePackedGuid(master);
        payload.WritePackedGuid(winner);

        var translated = WorldClient.ParseLootList(new WorldPacket(1, payload.GetData()), state);

        Assert.Equal(master.To128(state), translated.Master);
        Assert.Equal(winner.To128(state), translated.RoundRobinWinner);
    }

    [Fact]
    public void ParseLootResponse_PreservesFailureForForwarding()
    {
        var state = CreateGameState();
        var owner = new WowGuid64(HighGuidTypeLegacy.Creature, 1234, 42);
        var payload = new WorldPacket();
        payload.WriteGuid(owner);
        payload.WriteUInt8((byte)LootType.None);
        payload.WriteUInt8((byte)LootError.NoLoot);

        var translated = WorldClient.ParseLootResponse(new WorldPacket(1, payload.GetData()), state);

        Assert.Equal(LootType.None, translated.AcquireReason);
        Assert.Equal(LootError.NoLoot, translated.FailureReason);
        Assert.Equal(owner.To128(state), translated.Owner);
        Assert.Equal(owner.ToLootGuid(), translated.LootObj);
    }

    private static GameSessionData CreateGameState()
    {
        var global = (GlobalSessionData)RuntimeHelpers.GetUninitializedObject(typeof(GlobalSessionData));
        var state = GameSessionData.CreateNewGameSessionData(global);
        state.ObjectSpawnCount = new();
        return state;
    }
}

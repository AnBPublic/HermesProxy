using System.Runtime.CompilerServices;
using HermesProxy;
using HermesProxy.Enums;
using HermesProxy.World;
using HermesProxy.World.Enums;
using HermesProxy.World.Objects.Version.V3_4_3_54261;
using HermesProxy.World.Server.Packets;
using Xunit;

namespace HermesProxy.Tests.World;

public class WotlkObjectUpdateValuesTests
{
    [Fact]
    public void ValuesUpdate_HealthDeltaWritesUnitSection()
    {
        var guid = WowGuid128.Create(HighGuidType703.Creature, 0, 1234, 1);
        var update = new ObjectUpdate(guid, UpdateTypeModern.Values, CreateGlobalSession());
        update.UnitData.Health = 1234;

        var output = Write(update);

        Assert.Equal(UpdateTypeModern.Values, (UpdateTypeModern)output.ReadUInt8());
        Assert.Equal(guid, output.ReadPackedGuid128());
        var valuesLength = output.ReadUInt32();
        Assert.True(valuesLength > sizeof(uint));
        Assert.Equal(0x20u, output.ReadUInt32());
    }

    [Fact]
    public void ValuesUpdate_GameObjectDeltaWritesGameObjectSection()
    {
        var guid = WowGuid128.Create(HighGuidType703.GameObject, 0, 5678, 1);
        var update = new ObjectUpdate(guid, UpdateTypeModern.Values, CreateGlobalSession());
        update.GameObjectData.DisplayID = 7000;

        var output = Write(update);

        output.ReadUInt8();
        Assert.Equal(guid, output.ReadPackedGuid128());
        var valuesLength = output.ReadUInt32();
        Assert.True(valuesLength > sizeof(uint));
        Assert.Equal(0x100u, output.ReadUInt32());
    }

    [Fact]
    public void ValuesUpdate_ContainerDeltaWritesContainerSection()
    {
        var guid = WowGuid128.Create(HighGuidType703.Item, 0, 9012, 1);
        var state = CreateGameState();
        state.OriginalObjectTypes[guid] = ObjectType.Container;
        var update = new ObjectUpdate(guid, UpdateTypeModern.Values, CreateGlobalSession());
        update.ContainerData.Slots[0] = WowGuid128.Empty;

        var output = Write(update, state);

        output.ReadUInt8();
        Assert.Equal(guid, output.ReadPackedGuid128());
        var valuesLength = output.ReadUInt32();
        Assert.True(valuesLength > sizeof(uint));
        Assert.True((output.ReadUInt32() & 0x04u) != 0);
    }

    private static WorldPacket Write(ObjectUpdate update, GameSessionData? gameState = null)
    {
        var output = new WorldPacket();
        gameState ??= CreateGameState();
        new ObjectUpdateBuilder(update, gameState).WriteToPacket(output);
        return new WorldPacket(1, output.GetData());
    }

    private static GameSessionData CreateGameState()
    {
        var state = (GameSessionData)RuntimeHelpers.GetUninitializedObject(typeof(GameSessionData));
        state.OriginalObjectTypes = new();
        return state;
    }

    private static GlobalSessionData CreateGlobalSession()
    {
        return (GlobalSessionData)RuntimeHelpers.GetUninitializedObject(typeof(GlobalSessionData));
    }
}

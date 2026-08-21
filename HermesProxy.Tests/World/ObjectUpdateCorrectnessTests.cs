using System.Runtime.CompilerServices;
using HermesProxy.Enums;
using HermesProxy.World;
using HermesProxy.World.Enums;
using HermesProxy.World.Objects.Version.V3_4_3_54261;
using HermesProxy.World.Server.Packets;
using Xunit;

namespace HermesProxy.Tests.World;

public sealed class ObjectUpdateCorrectnessTests
{
    [Fact]
    public void FailureDiagnosticCorrelatesLatestGuidReuseAttempt()
    {
        var guid = WowGuid128.Create(HighGuidType703.GameObject, 0, 1234, 7);
        var tracker = new ObjectUpdateDiagnosticTracker();

        tracker.Record(guid, ObjectType.GameObject, UpdateTypeModern.CreateObject1, "Object,GameObject", "fixtures/create-gameobject.json");
        tracker.Record(guid, ObjectType.GameObject, UpdateTypeModern.Values, "Object,GameObject", "fixtures/values-gameobject.json");

        var message = tracker.DescribeFailure(guid);

        Assert.Contains("objectType=GameObject", message);
        Assert.Contains("guidCategory=GameObject", message);
        Assert.Contains("updateKind=Values", message);
        Assert.Contains("serializerSection=Object,GameObject", message);
        Assert.Contains("fixture=fixtures/values-gameobject.json", message);
    }

    [Fact]
    public void FilterRemovesDestroyedAndOutOfRangeGuidsBeforeReuse()
    {
        var state = CreateGameState();
        var destroyed = WowGuid128.Create(HighGuidType703.Creature, 0, 100, 1);
        var outOfRange = WowGuid128.Create(HighGuidType703.GameObject, 0, 200, 2);
        state.ClientKnownGuids.Add(destroyed);
        state.ClientKnownGuids.Add(outOfRange);
        var packet = new UpdateObject(state);
        packet.DestroyedGuids.Add(destroyed);
        packet.OutOfRangeGuids.Add(outOfRange);

        UpdateObject.FilterV3_4_3ValuesCore(packet, state);

        Assert.DoesNotContain(destroyed, state.ClientKnownGuids);
        Assert.DoesNotContain(outOfRange, state.ClientKnownGuids);
    }

    [Fact]
    public void FilterKeepsCreateThenValuesForSameGuidInOneBatch()
    {
        var state = CreateGameState();
        var guid = WowGuid128.Create(HighGuidType703.Creature, 0, 100, 3);
        var packet = new UpdateObject(state);
        packet.ObjectUpdates.Add(new ObjectUpdate(guid, UpdateTypeModern.CreateObject1, CreateGlobalSession()));
        var values = new ObjectUpdate(guid, UpdateTypeModern.Values, CreateGlobalSession());
        values.UnitData.Health = 90;
        packet.ObjectUpdates.Add(values);

        var removed = UpdateObject.FilterV3_4_3ValuesCore(packet, state);

        Assert.Equal(0, removed);
        Assert.Equal(2, packet.ObjectUpdates.Count);
        Assert.Contains(guid, state.ClientKnownGuids);
    }

    [Fact]
    public void FilterDropsEmptyValuesMask()
    {
        var state = CreateGameState();
        var guid = WowGuid128.Create(HighGuidType703.Creature, 0, 100, 4);
        state.ClientKnownGuids.Add(guid);
        var packet = new UpdateObject(state);
        packet.ObjectUpdates.Add(new ObjectUpdate(guid, UpdateTypeModern.Values, CreateGlobalSession()));

        var removed = UpdateObject.FilterV3_4_3ValuesCore(packet, state);

        Assert.Equal(1, removed);
        Assert.Empty(packet.ObjectUpdates);
    }

    [Fact]
    public void VisibilityFlagsSeparateOwnerPartyMemberAndNonOwner()
    {
        var state = CreateGameState();
        var owner = WowGuid128.Create(HighGuidType703.Player, 0, 100, 5);
        var partyMember = WowGuid128.Create(HighGuidType703.Player, 0, 100, 6);
        var stranger = WowGuid128.Create(HighGuidType703.Player, 0, 100, 7);
        state.CurrentPlayerGuid = owner;
        state.CurrentGroups[0] = new PartyUpdate();
        state.CurrentGroups[0]!.PlayerList.Add(new PartyPlayerInfo { GUID = owner });
        state.CurrentGroups[0]!.PlayerList.Add(new PartyPlayerInfo { GUID = partyMember });

        Assert.Equal(0x03, new ObjectUpdateBuilder(new ObjectUpdate(owner, UpdateTypeModern.Values, CreateGlobalSession()), state).UpdateFieldVisibilityFlags);
        Assert.Equal(0x02, new ObjectUpdateBuilder(new ObjectUpdate(partyMember, UpdateTypeModern.Values, CreateGlobalSession()), state).UpdateFieldVisibilityFlags);
        Assert.Equal(0x00, new ObjectUpdateBuilder(new ObjectUpdate(stranger, UpdateTypeModern.Values, CreateGlobalSession()), state).UpdateFieldVisibilityFlags);
    }

    private static GameSessionData CreateGameState()
    {
        var state = (GameSessionData)RuntimeHelpers.GetUninitializedObject(typeof(GameSessionData));
        state.ClientKnownGuids = [];
        state.OriginalObjectTypes = [];
        state.ObjectUpdateDiagnostics = new();
        state.CurrentGroups = new PartyUpdate?[2];
        return state;
    }

    private static GlobalSessionData CreateGlobalSession()
        => (GlobalSessionData)RuntimeHelpers.GetUninitializedObject(typeof(GlobalSessionData));
}

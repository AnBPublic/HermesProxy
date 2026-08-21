using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Framework.Constants;
using HermesProxy.World;
using HermesProxy.World.Enums;
using HermesProxy.World.Server.Packets;

namespace HermesProxy.Tests.ProtocolReplay;

public static class PacketReplayHarness
{
    public static PacketReplayResult Replay(PacketFixture fixture)
    {
        try
        {
            return fixture.Translation switch
            {
                "legacy.loot-list" => ReplayLootList(fixture),
                "modern.close-interaction" => ReplayCloseInteraction(fixture),
                _ => throw new InvalidDataException($"Unknown replay translation: {fixture.Translation}")
            };
        }
        catch (Exception) when (fixture.Case is PacketCase.Truncated or PacketCase.UnexpectedValue or PacketCase.Fuzz)
        {
            return Rejected(fixture.Case switch { PacketCase.Truncated => "truncated", PacketCase.Fuzz => "fuzz", _ => UnexpectedFailure(fixture) });
        }
    }

    private static PacketReplayResult ReplayLootList(PacketFixture fixture)
    {
        using var source = new WorldPacket(PacketFixtureCorpus.ParseOpcode(fixture.WireOpcode), PacketFixtureCorpus.ParseHex(fixture.SourcePayloadHex));
        var state = CreateGameState();
        var owner = source.ReadGuid();
        var owner128 = owner.To128(state);
        var lootObject = owner.ToLootGuid();
        var master = source.ReadPackedGuid();
        var master128 = master.IsEmpty() ? default : master.To128(state);
        var roundRobin = source.ReadPackedGuid();
        var roundRobin128 = roundRobin.IsEmpty() ? default : roundRobin.To128(state);
        if (owner128.IsEmpty()) return Rejected("empty-owner");

        var translated = CreateLootListPacket(owner128, lootObject, master128, roundRobin128);
        translated.WritePacketData();
        return new(ReplayOutcome.Translated, translated.GetOpcode(), translated.GetConnection().ToString(), translated.GetData() ?? [],
            Fields(("owner.low", owner128.Low), ("owner.high", owner128.High), ("lootObject.low", lootObject.Low), ("lootObject.high", lootObject.High), ("master.low", master128.Low), ("roundRobin.low", roundRobin128.Low)));
    }

    private static PacketReplayResult ReplayCloseInteraction(PacketFixture fixture)
    {
        using var source = new WorldPacket(PacketFixtureCorpus.ParseOpcode(fixture.WireOpcode), PacketFixtureCorpus.ParseHex(fixture.SourcePayloadHex));
        using var parsed = new CloseInteraction(source);
        parsed.Read();
        if (parsed.SourceGuid.IsEmpty()) return Rejected("empty-source");
        using var translated = new WorldPacket(Opcode.CMSG_QUEST_GIVER_CANCEL);
        return new(ReplayOutcome.Translated, translated.GetOpcode(), ConnectionType.Realm.ToString(), (byte[])translated.GetData().Clone(),
            Fields(("source.low", parsed.SourceGuid.Low), ("source.high", parsed.SourceGuid.High), ("optionalFields", "none")));
    }

    private static PacketReplayResult Rejected(string failure) => new(ReplayOutcome.Rejected, 0, ConnectionType.Instance.ToString(), [], Fields(("failure", failure)));
    private static ServerPacket CreateLootListPacket(WowGuid128 owner, WowGuid128 lootObject, WowGuid128 master, WowGuid128 roundRobin)
    {
        var type = typeof(ServerPacket).Assembly.GetType("HermesProxy.World.Server.Packets.LootList", throwOnError: true)!;
        var packet = (ServerPacket)Activator.CreateInstance(type)!;
        type.GetField("Owner", BindingFlags.Instance | BindingFlags.Public)!.SetValue(packet, owner);
        type.GetField("LootObj", BindingFlags.Instance | BindingFlags.Public)!.SetValue(packet, lootObject);
        type.GetField("Master", BindingFlags.Instance | BindingFlags.Public)!.SetValue(packet, master);
        type.GetField("RoundRobinWinner", BindingFlags.Instance | BindingFlags.Public)!.SetValue(packet, roundRobin);
        return packet;
    }
    private static string UnexpectedFailure(PacketFixture fixture) => fixture.Translation switch { "legacy.loot-list" => "empty-owner", "modern.close-interaction" => "empty-source", _ => "unexpected-value" };
    private static Dictionary<string, JsonElement> Fields(params (string Name, object Value)[] fields) => fields.ToDictionary(field => field.Name, field => JsonSerializer.SerializeToElement(Convert.ToString(field.Value, CultureInfo.InvariantCulture) ?? string.Empty), StringComparer.Ordinal);
    private static GameSessionData CreateGameState()
    {
        var global = (GlobalSessionData)RuntimeHelpers.GetUninitializedObject(typeof(GlobalSessionData));
        var state = GameSessionData.CreateNewGameSessionData(global); state.ObjectSpawnCount = new(); return state;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace HermesProxy.Tests.ProtocolReplay;

public enum PacketDirection { LegacyToModern, ModernToLegacy }
public enum PacketCase { Positive, MissingOptionalField, Truncated, UnexpectedValue, Fuzz }
public enum BackendDialect { Common, CMaNGOS, TrinityCore, AzerothCore, Maelstrom }
public enum ReplayOutcome { Translated, Rejected }

public sealed record PacketEvidence(string Kind, string Source, bool Sanitized);
public sealed record ExpectedPacket(ReplayOutcome Outcome, string WireOpcode, string ConnectionType, string PayloadHex, Dictionary<string, JsonElement> SemanticFields);
public sealed record PacketFixture(int SchemaVersion, string Id, string Flow, PacketCase Case, string Translation, PacketDirection Direction, string WireOpcode, string ConnectionType, BackendDialect BackendDialect, IReadOnlyList<BackendDialect> ObservedDialects, string ModernBuild, string LegacyBuild, string SourcePayloadHex, ExpectedPacket Expected, PacketEvidence Evidence);

public sealed record PacketReplayResult(ReplayOutcome Outcome, uint WireOpcode, string ConnectionType, byte[] Payload, Dictionary<string, JsonElement> SemanticFields)
{
    public string Canonical() => string.Join('|', Outcome, WireOpcode.ToString("X8"), ConnectionType, Convert.ToHexString(Payload),
        string.Join(';', SemanticFields.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => $"{pair.Key}={pair.Value.GetRawText()}")));
}

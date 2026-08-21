using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HermesProxy.Tests.ProtocolReplay;

public static class PacketFixtureCorpus
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public static IReadOnlyList<PacketFixture> Load()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Protocol");
        if (!Directory.Exists(root)) throw new DirectoryNotFoundException($"Protocol fixture directory not found: {root}");
        var fixtures = Directory.EnumerateFiles(root, "*.json", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.Ordinal).SelectMany(LoadFile)
            .OrderBy(fixture => fixture.Id, StringComparer.Ordinal).ToArray();
        if (fixtures.Length == 0) throw new InvalidDataException("Protocol fixture corpus is empty.");
        if (fixtures.Select(fixture => fixture.Id).Distinct(StringComparer.Ordinal).Count() != fixtures.Length)
            throw new InvalidDataException("Protocol fixture IDs must be unique.");
        return fixtures;
    }

    private static IReadOnlyList<PacketFixture> LoadFile(string path)
    {
        var fixtures = JsonSerializer.Deserialize<PacketFixture[]>(File.ReadAllText(path), JsonOptions)
            ?? throw new InvalidDataException($"Fixture file deserialized to null: {path}");
        foreach (var fixture in fixtures) Validate(fixture, path);
        return fixtures;
    }

    private static void Validate(PacketFixture fixture, string path)
    {
        if (fixture.SchemaVersion != 1) throw new InvalidDataException($"{path}: {fixture.Id} uses unsupported schema {fixture.SchemaVersion}.");
        if (fixture.ModernBuild != "V3_4_3_54261" || fixture.LegacyBuild != "V3_3_5a_12340") throw new InvalidDataException($"{path}: {fixture.Id} targets the wrong protocol pair.");
        if (string.IsNullOrWhiteSpace(fixture.Id) || string.IsNullOrWhiteSpace(fixture.Translation)) throw new InvalidDataException($"{path}: fixture ID and translation are required.");
        if (!fixture.Evidence.Sanitized || string.IsNullOrWhiteSpace(fixture.Evidence.Source)) throw new InvalidDataException($"{path}: {fixture.Id} must carry sanitized evidence provenance.");
        if (fixture.ObservedDialects.Count == 0 || (!fixture.ObservedDialects.Contains(fixture.BackendDialect) && fixture.BackendDialect != BackendDialect.Common)) throw new InvalidDataException($"{path}: {fixture.Id} must identify its observed backend dialect.");
        if (fixture.Expected.SemanticFields.Count == 0) throw new InvalidDataException($"{path}: {fixture.Id} must include expected semantic fields.");
        ParseOpcode(fixture.WireOpcode); ParseOpcode(fixture.Expected.WireOpcode); ParseHex(fixture.SourcePayloadHex); ParseHex(fixture.Expected.PayloadHex);
    }

    public static uint ParseOpcode(string value)
    {
        if (!value.StartsWith("0x", StringComparison.OrdinalIgnoreCase) || !uint.TryParse(value.AsSpan(2), System.Globalization.NumberStyles.HexNumber, null, out var opcode))
            throw new InvalidDataException($"Invalid wire opcode: {value}");
        return opcode;
    }

    public static byte[] ParseHex(string value)
    {
        if ((value.Length & 1) != 0) throw new InvalidDataException("Packet payload hex must contain complete bytes.");
        try { return Convert.FromHexString(value); }
        catch (FormatException exception) { throw new InvalidDataException("Packet payload contains non-hex characters.", exception); }
    }
}

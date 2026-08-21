using System;
using System.Collections.Generic;
using System.Linq;
using Xunit.Sdk;

namespace HermesProxy.Tests.ProtocolReplay;

public sealed class PacketReplayMismatchException(string message) : XunitException(message);

public static class PacketReplayAssert
{
    public static void Matches(PacketFixture fixture, PacketReplayResult actual)
    {
        var differences = new List<string>();
        var expectedPayload = PacketFixtureCorpus.ParseHex(fixture.Expected.PayloadHex);
        var expectedOpcode = PacketFixtureCorpus.ParseOpcode(fixture.Expected.WireOpcode);
        if (actual.Outcome != fixture.Expected.Outcome) differences.Add($"outcome: expected {fixture.Expected.Outcome}, actual {actual.Outcome}");
        if (actual.WireOpcode != expectedOpcode) differences.Add($"wireOpcode: expected 0x{expectedOpcode:X}, actual 0x{actual.WireOpcode:X}");
        if (!string.Equals(actual.ConnectionType, fixture.Expected.ConnectionType, StringComparison.Ordinal)) differences.Add($"connectionType: expected {fixture.Expected.ConnectionType}, actual {actual.ConnectionType}");
        if (!actual.Payload.AsSpan().SequenceEqual(expectedPayload)) differences.Add(DescribeByteDifference(expectedPayload, actual.Payload));
        foreach (var expectedField in fixture.Expected.SemanticFields.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            if (!actual.SemanticFields.TryGetValue(expectedField.Key, out var actualValue)) differences.Add($"semantic.{expectedField.Key}: missing");
            else if (actualValue.GetRawText() != expectedField.Value.GetRawText()) differences.Add($"semantic.{expectedField.Key}: expected {expectedField.Value.GetRawText()}, actual {actualValue.GetRawText()}");
        }
        if (differences.Count != 0) throw new PacketReplayMismatchException($"Fixture {fixture.Id} failed:\n{string.Join("\n", differences)}");
    }

    private static string DescribeByteDifference(byte[] expected, byte[] actual)
    {
        var common = Math.Min(expected.Length, actual.Length); var offset = 0;
        while (offset < common && expected[offset] == actual[offset]) offset++;
        return offset == common ? $"payload: expected {expected.Length} bytes, actual {actual.Length} bytes" : $"payload[{offset}]: expected 0x{expected[offset]:X2}, actual 0x{actual[offset]:X2}";
    }
}

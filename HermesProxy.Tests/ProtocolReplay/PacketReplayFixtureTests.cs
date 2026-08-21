using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace HermesProxy.Tests.ProtocolReplay;

public sealed class PacketReplayFixtureTests
{
    [Fact]
    public void CorpusReplaysThroughHermesParsersInBothDirections()
    {
        var fixtures = PacketFixtureCorpus.Load();
        Assert.Contains(fixtures, fixture => fixture.Direction == PacketDirection.LegacyToModern);
        Assert.Contains(fixtures, fixture => fixture.Direction == PacketDirection.ModernToLegacy);

        foreach (var fixture in fixtures)
            PacketReplayAssert.Matches(fixture, PacketReplayHarness.Replay(fixture));
    }

    [Fact]
    public void CorpusIsDeterministicAcrossRepeatedAndParallelRuns()
    {
        var fixtures = PacketFixtureCorpus.Load();
        var expected = fixtures.ToDictionary(fixture => fixture.Id, PacketReplayHarness.Replay);
        var failures = new ConcurrentQueue<string>();

        Parallel.ForEach(fixtures, fixture =>
        {
            for (var iteration = 0; iteration < 8; iteration++)
            {
                var actual = PacketReplayHarness.Replay(fixture);
                if (expected[fixture.Id].Canonical() != actual.Canonical())
                    failures.Enqueue($"{fixture.Id} changed on iteration {iteration}");
            }
        });

        Assert.Empty(failures);
    }

    [Fact]
    public void CorpusCarriesRequiredProtocolAndDialectMetadata()
    {
        var fixtures = PacketFixtureCorpus.Load();
        var dialects = fixtures.SelectMany(fixture => fixture.ObservedDialects).ToHashSet();

        Assert.All(fixtures, fixture =>
        {
            Assert.Equal(1, fixture.SchemaVersion);
            Assert.Equal("V3_4_3_54261", fixture.ModernBuild);
            Assert.Equal("V3_3_5a_12340", fixture.LegacyBuild);
            Assert.False(string.IsNullOrWhiteSpace(fixture.ConnectionType));
            Assert.False(string.IsNullOrWhiteSpace(fixture.Expected.ConnectionType));
            Assert.NotEmpty(fixture.Expected.SemanticFields);
            Assert.True(fixture.Evidence.Sanitized);
            Assert.DoesNotContain("account", fixture.SourcePayloadHex, StringComparison.OrdinalIgnoreCase);
        });

        Assert.Contains(BackendDialect.CMaNGOS, dialects);
        Assert.Contains(BackendDialect.TrinityCore, dialects);
        Assert.Contains(BackendDialect.AzerothCore, dialects);
        Assert.Contains(BackendDialect.Maelstrom, dialects);
        Assert.Contains(fixtures, fixture => fixture.Case == PacketCase.Fuzz && fixture.Expected.Outcome == ReplayOutcome.Rejected);
    }

    [Theory]
    [InlineData("legacy.loot-list")]
    [InlineData("modern.close-interaction")]
    public void SeededP0OpcodesHaveRequiredCases(string translation)
    {
        var cases = PacketFixtureCorpus.Load()
            .Where(fixture => fixture.Translation == translation)
            .Select(fixture => fixture.Case)
            .ToHashSet();

        Assert.Contains(PacketCase.Positive, cases);
        Assert.Contains(PacketCase.MissingOptionalField, cases);
        Assert.Contains(PacketCase.Truncated, cases);
        Assert.Contains(PacketCase.UnexpectedValue, cases);
    }

    [Fact]
    public void SemanticMismatchNamesTheDamagedField()
    {
        var fixture = PacketFixtureCorpus.Load().First(fixture => fixture.Id == "loot-list-positive");
        var damaged = PacketReplayHarness.Replay(fixture) with
        {
            SemanticFields = new Dictionary<string, JsonElement>(fixture.Expected.SemanticFields)
            {
                ["master.low"] = JsonSerializer.SerializeToElement("999")
            }
        };

        var error = Assert.Throws<PacketReplayMismatchException>(() => PacketReplayAssert.Matches(fixture, damaged));
        Assert.Contains("master.low", error.Message, StringComparison.Ordinal);
    }
}

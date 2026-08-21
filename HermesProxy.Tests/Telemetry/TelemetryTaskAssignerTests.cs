using System;
using System.Collections.Generic;
using HermesProxy.Telemetry;
using Xunit;

namespace HermesProxy.Tests.Telemetry;

public sealed class TelemetryTaskAssignerTests
{
    [Fact]
    public void UnknownLootListGetsP0RepairTask()
    {
        var snapshot = Snapshot(new Dictionary<string, long>
        {
            ["unknown_opcode.SMSG_LOOT_LIST"] = 1
        });

        var task = Assert.Single(TelemetryTaskAssigner.Assign(snapshot));

        Assert.Equal("HERMES-WOTLK-LOOT-LIST", task.Id);
        Assert.Equal("P0", task.Priority);
    }

    [Fact]
    public void ObjectUpdateFailureAndQueryTimeoutGetAutomaticTasks()
    {
        var snapshot = Snapshot(new Dictionary<string, long>
        {
            ["object_update_failed"] = 1,
            ["item_query_timeout"] = 2
        });

        var tasks = TelemetryTaskAssigner.Assign(snapshot);

        Assert.Collection(tasks,
            task => Assert.Equal("HERMES-WOTLK-OBJECT-UPDATE", task.Id),
            task => Assert.Equal("HERMES-WOTLK-ITEM-QUERY-STALL", task.Id));
    }

    [Fact]
    public void HighSelectionAndLootP95GetLatencyTasks()
    {
        var snapshot = new TelemetrySnapshot(
            "session",
            DateTimeOffset.UtcNow,
            new Dictionary<string, long>(),
            new Dictionary<string, TelemetryLatencySummary>
            {
                ["Selection"] = new(4, 51, 60, 80, 20),
                ["Loot"] = new(4, 101, 130, 140, 90)
            },
            []);

        var tasks = TelemetryTaskAssigner.Assign(snapshot);

        Assert.Contains(tasks, task => task.Id == "HERMES-WOTLK-TARGET-LATENCY");
        Assert.Contains(tasks, task => task.Id == "HERMES-WOTLK-LOOT-LATENCY");
    }

    [Fact]
    public void NetworkAndCompatibilitySignalsGetTasks()
    {
        var snapshot = Snapshot(new Dictionary<string, long>
        {
            ["nagle_requested"] = 1,
            ["connection_unexpected_disconnect"] = 1,
            ["movement_unknown"] = 1,
            ["spell_unknown"] = 1,
            ["interaction_close_unknown"] = 1
        });

        var tasks = TelemetryTaskAssigner.Assign(snapshot);

        Assert.Contains(tasks, task => task.Id == "HERMES-WOTLK-NAGLE");
        Assert.Contains(tasks, task => task.Id == "HERMES-WOTLK-CONNECTION-STABILITY");
        Assert.Contains(tasks, task => task.Id == "HERMES-WOTLK-MOVEMENT-COMPATIBILITY");
        Assert.Contains(tasks, task => task.Id == "HERMES-WOTLK-SPELL-COMPATIBILITY");
        Assert.Contains(tasks, task => task.Id == "HERMES-WOTLK-INTERACTION-CLOSE");
    }

    [Fact]
    public void CleanSnapshotDoesNotCreateFalsePositiveTasks()
    {
        Assert.Empty(TelemetryTaskAssigner.Assign(Snapshot(new Dictionary<string, long>())));
    }

    private static TelemetrySnapshot Snapshot(Dictionary<string, long> counters) => new(
        "session",
        DateTimeOffset.UtcNow,
        counters,
        new Dictionary<string, TelemetryLatencySummary>(),
        []);
}

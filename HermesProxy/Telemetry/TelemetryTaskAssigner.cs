using System;
using System.Collections.Generic;
using System.Linq;

namespace HermesProxy.Telemetry;

public static class TelemetryTaskAssigner
{
    public static IReadOnlyList<AssignedTelemetryTask> Assign(TelemetrySnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var tasks = new List<AssignedTelemetryTask>();

        if (HasAny(snapshot, "unknown_opcode.SMSG_LOOT_LIST", "loot_list_parse_failure"))
        {
            tasks.Add(new AssignedTelemetryTask(
                "HERMES-WOTLK-LOOT-LIST",
                "P0",
                "Repair the SMSG_LOOT_LIST translation path",
                "The legacy loot-list path was unknown or failed to parse; loot results cannot be trusted until this is fixed.",
                Evidence(snapshot, "unknown_opcode.SMSG_LOOT_LIST", "loot_list_parse_failure")));
        }

        if (HasAny(snapshot, "object_update_failed", "object_update_parse_failure", "unresolved_required_template"))
        {
            tasks.Add(new AssignedTelemetryTask(
                "HERMES-WOTLK-OBJECT-UPDATE",
                "P0",
                "Repair object-update translation and template resolution",
                "The modern client reported an object-update failure or Hermes could not produce a complete update.",
                Evidence(snapshot, "object_update_failed", "object_update_parse_failure", "unresolved_required_template")));
        }

        if (HasAny(snapshot, "item_query_timeout"))
        {
            tasks.Add(new AssignedTelemetryTask(
                "HERMES-WOTLK-ITEM-QUERY-STALL",
                "P1",
                "Remove deferred item-query stalls",
                "An object update waited for item data until the safety timeout and was released incomplete.",
                Evidence(snapshot, "item_query_timeout")));
        }

        if (IsSlow(snapshot, "Selection", 50) || HasAny(snapshot, "lifecycle_incomplete.Selection"))
        {
            tasks.Add(new AssignedTelemetryTask(
                "HERMES-WOTLK-TARGET-LATENCY",
                "P1",
                "Reduce target-selection round-trip latency",
                "Selection lifecycle p95 exceeded 50 ms or a selection lifecycle did not complete.",
                Evidence(snapshot, "Selection", "lifecycle_incomplete.Selection")));
        }

        if (IsSlow(snapshot, "Loot", 100) || IsSlow(snapshot, "LootItem", 100) ||
            HasAny(snapshot, "lifecycle_incomplete.Loot", "lifecycle_incomplete.LootItem"))
        {
            tasks.Add(new AssignedTelemetryTask(
                "HERMES-WOTLK-LOOT-LATENCY",
                "P1",
                "Reduce loot and loot-item round-trip latency",
                "Loot or loot-item lifecycle p95 exceeded 100 ms or a loot lifecycle did not complete.",
                Evidence(snapshot, "Loot", "LootItem", "lifecycle_incomplete.Loot", "lifecycle_incomplete.LootItem")));
        }

        if (HasAny(snapshot, "nagle_requested", "socket_no_delay_false"))
        {
            tasks.Add(new AssignedTelemetryTask(
                "HERMES-WOTLK-NAGLE",
                "P1",
                "Keep TCP_NODELAY enabled on both proxy legs",
                "The client requested Nagle behavior or an active proxy socket reported NoDelay=false.",
                Evidence(snapshot, "nagle_requested", "socket_no_delay_false")));
        }

        if (HasAny(snapshot, "connection_unexpected_disconnect", "write_failure"))
        {
            tasks.Add(new AssignedTelemetryTask(
                "HERMES-WOTLK-CONNECTION-STABILITY",
                "P1",
                "Repair unexpected proxy connection or write failures",
                "The session recorded an unexpected disconnect or a failed socket write.",
                Evidence(snapshot, "connection_unexpected_disconnect", "write_failure")));
        }

        if (HasAny(snapshot, "movement_unknown"))
        {
            tasks.Add(new AssignedTelemetryTask(
                "HERMES-WOTLK-MOVEMENT-COMPATIBILITY",
                "P2",
                "Capture and translate the remaining movement compatibility path",
                "Movement timing or collision-related traffic was not recognized by the active translation profile.",
                Evidence(snapshot, "movement_unknown")));
        }

        if (HasAny(snapshot, "spell_unknown"))
        {
            tasks.Add(new AssignedTelemetryTask(
                "HERMES-WOTLK-SPELL-COMPATIBILITY",
                "P2",
                "Capture and translate the remaining spell compatibility path",
                "Spell execute or impact traffic was not recognized by the active translation profile.",
                Evidence(snapshot, "spell_unknown")));
        }

        if (HasAny(snapshot, "interaction_close_unknown"))
        {
            tasks.Add(new AssignedTelemetryTask(
                "HERMES-WOTLK-INTERACTION-CLOSE",
                "P2",
                "Complete close-interaction translation",
                "The client sent a close-interaction signal that the active translation profile did not recognize.",
                Evidence(snapshot, "interaction_close_unknown")));
        }

        return tasks;
    }

    private static bool IsSlow(TelemetrySnapshot snapshot, string flow, double p95Threshold)
        => snapshot.Lifecycles.TryGetValue(flow, out var stats) && stats.Count > 0 && stats.P95 > p95Threshold;

    private static bool HasAny(TelemetrySnapshot snapshot, params string[] keys)
        => keys.Any(key => snapshot.Counters.TryGetValue(key, out var count) && count > 0);

    private static IReadOnlyList<string> Evidence(TelemetrySnapshot snapshot, params string[] keys)
    {
        var evidence = new List<string>();
        foreach (var key in keys)
        {
            if (snapshot.Counters.TryGetValue(key, out var count) && count > 0)
                evidence.Add($"counter:{key}={count}");
            else if (snapshot.Lifecycles.TryGetValue(key, out var stats) && stats.Count > 0)
                evidence.Add($"lifecycle:{key};count={stats.Count};p95Ms={stats.P95:F3};p99Ms={stats.P99:F3}");
        }

        return evidence;
    }
}

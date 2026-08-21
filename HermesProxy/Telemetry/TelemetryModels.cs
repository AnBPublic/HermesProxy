using System;
using System.Collections.Generic;

namespace HermesProxy.Telemetry;

public sealed record TelemetryEvidence(
    DateTimeOffset TimestampUtc,
    string EventType,
    string? Direction,
    string? Opcode,
    string? Detail);

public sealed record TelemetryLatencySummary(
    int Count,
    double P95,
    double P99,
    double Max,
    double LegacyWaitP95);

public sealed record TelemetrySnapshot(
    string SessionId,
    DateTimeOffset CapturedAtUtc,
    IReadOnlyDictionary<string, long> Counters,
    IReadOnlyDictionary<string, TelemetryLatencySummary> Lifecycles,
    IReadOnlyList<TelemetryEvidence> RecentEvidence,
    string? ClientBuild = null,
    string? LegacyBuild = null);

public sealed record AssignedTelemetryTask(
    string Id,
    string Priority,
    string Title,
    string Reason,
    IReadOnlyList<string> Evidence);

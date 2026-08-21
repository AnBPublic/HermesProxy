using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using Framework.Metrics;
using HermesProxy.Configuration.Options;

namespace HermesProxy.Telemetry;

public sealed class ProxyTelemetry : IDisposable
{
    private static readonly string[] LifecycleNames = [
        "Selection", "Loot", "LootItem", "LootResponse", "LootList", "ObjectUpdate" ];

    private readonly ProxyMetrics _metrics;
    private readonly ConcurrentQueue<TelemetryEvidence> _pending = new();
    private readonly ConcurrentDictionary<string, long> _counters = new(StringComparer.Ordinal);
    private readonly object _flushGate = new();
    private readonly int _maxBufferedEvents;
    private bool _disposed;

    public ProxyTelemetry(
        TelemetryOptions options,
        ProxyMetrics metrics,
        string? clientBuild,
        string? legacyBuild)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(metrics);

        Enabled = options.Enabled;
        SessionId = SanitizeSessionId(options.SessionId);
        _metrics = metrics;
        ClientBuild = clientBuild;
        LegacyBuild = legacyBuild;
        _maxBufferedEvents = Math.Max(128, options.MaxBufferedEvents);

        var root = string.IsNullOrWhiteSpace(options.Directory)
            ? Path.Combine(AppContext.BaseDirectory, "Logs", "Telemetry")
            : options.Directory;
        DirectoryPath = Path.GetFullPath(Path.IsPathRooted(root)
            ? root
            : Path.Combine(AppContext.BaseDirectory, root));
        EventsPath = Path.Combine(DirectoryPath, "events.jsonl");
        SummaryPath = Path.Combine(DirectoryPath, "summary.json");
        TaskQueuePath = Path.Combine(DirectoryPath, "task-queue.json");

        if (Enabled)
        {
            try { Directory.CreateDirectory(DirectoryPath); }
            catch { _counters["telemetry_directory_failure"] = 1; }
        }
    }

    public bool Enabled { get; }
    public string SessionId { get; }
    public string DirectoryPath { get; }
    public string EventsPath { get; }
    public string SummaryPath { get; }
    public string TaskQueuePath { get; }
    public string? ClientBuild { get; }
    public string? LegacyBuild { get; }

    /// <summary>
    /// Enqueues metadata only. This method never touches the filesystem.
    /// </summary>
    public void Record(string eventType, string? direction = null, string? opcode = null, string? detail = null)
    {
        if (!Enabled || _disposed || string.IsNullOrWhiteSpace(eventType))
            return;

        Increment(eventType);
        if (!string.IsNullOrWhiteSpace(direction))
            Increment($"{eventType}.{direction}");
        if (!string.IsNullOrWhiteSpace(opcode))
            Increment($"{eventType}.{opcode}");

        var evidence = new TelemetryEvidence(
            DateTimeOffset.UtcNow,
            NormalizeToken(eventType) ?? "event",
            NormalizeToken(direction),
            NormalizeToken(opcode),
            NormalizeDetail(detail));

        if (Interlocked.Increment(ref _pendingCount) > _maxBufferedEvents)
        {
            Interlocked.Decrement(ref _pendingCount);
            Increment("telemetry_dropped_events");
            return;
        }

        _pending.Enqueue(evidence);
    }

    private long _pendingCount;

    public void Flush()
    {
        if (!Enabled || _disposed)
            return;

        lock (_flushGate)
        {
            if (_disposed)
                return;

            var drained = new List<TelemetryEvidence>();
            while (_pending.TryDequeue(out var evidence))
            {
                Interlocked.Decrement(ref _pendingCount);
                drained.Add(evidence);
            }

            try
            {
                Directory.CreateDirectory(DirectoryPath);
                if (drained.Count > 0)
                {
                    var jsonl = new StringBuilder();
                    foreach (var evidence in drained)
                    {
                        jsonl.AppendLine(JsonSerializer.Serialize(evidence));
                    }

                    File.AppendAllText(EventsPath, jsonl.ToString());
                }

                var snapshot = BuildSnapshot(drained);
                File.WriteAllText(SummaryPath, JsonSerializer.Serialize(snapshot, JsonOptions));
                var tasks = TelemetryTaskAssigner.Assign(snapshot);
                var taskQueue = new
                {
                    snapshot.SessionId,
                    snapshot.CapturedAtUtc,
                    Tasks = tasks,
                    EvidenceFiles = new[] { EventsPath, SummaryPath }
                };
                File.WriteAllText(TaskQueuePath, JsonSerializer.Serialize(taskQueue, JsonOptions));
            }
            catch
            {
                Increment("telemetry_write_failure");
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        Flush();
        _disposed = true;
    }

    private TelemetrySnapshot BuildSnapshot(IReadOnlyList<TelemetryEvidence> recentEvidence)
    {
        var lifecycles = new Dictionary<string, TelemetryLatencySummary>(StringComparer.OrdinalIgnoreCase);
        foreach (var flow in LifecycleNames)
        {
            var stats = _metrics.GetLifecycleStats(flow);
            if (stats is not { } value || value.Count == 0)
                continue;

            lifecycles[flow] = new TelemetryLatencySummary(
                value.Count,
                value.EndToEnd.P95,
                value.EndToEnd.P99,
                value.EndToEnd.Max,
                value.LegacyWait.P95);
        }

        return new TelemetrySnapshot(
            SessionId,
            DateTimeOffset.UtcNow,
            _counters.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal),
            lifecycles,
            recentEvidence,
            ClientBuild,
            LegacyBuild);
    }

    private void Increment(string key)
        => _counters.AddOrUpdate(key, 1, static (_, current) => current + 1);

    private static string SanitizeSessionId(string? sessionId)
    {
        var candidate = string.IsNullOrWhiteSpace(sessionId) ? Guid.NewGuid().ToString("N") : sessionId.Trim();
        var builder = new StringBuilder(Math.Min(candidate.Length, 96));
        foreach (var character in candidate)
        {
            if (builder.Length >= 96)
                break;
            if (char.IsLetterOrDigit(character) || character is '-' or '_')
                builder.Append(character);
        }

        return builder.Length == 0 ? Guid.NewGuid().ToString("N") : builder.ToString();
    }

    private static string? NormalizeToken(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? NormalizeDetail(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var normalized = value.Replace('\r', ' ').Replace('\n', ' ').Trim();
        return normalized.Length <= 256 ? normalized : normalized[..256];
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };
}

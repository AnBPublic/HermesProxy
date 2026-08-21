using System;
using System.IO;
using System.Text.Json;
using Framework.Metrics;
using HermesProxy.Configuration.Options;
using HermesProxy.Telemetry;
using Xunit;

namespace HermesProxy.Tests.Telemetry;

public sealed class ProxyTelemetryTests
{
    [Fact]
    public void FlushWritesMetadataOnlyEvidenceAndAutomaticTaskQueue()
    {
        var root = Path.Combine(Path.GetTempPath(), "hermes-telemetry-" + Guid.NewGuid().ToString("N"));
        try
        {
            using var telemetry = new ProxyTelemetry(
                new TelemetryOptions { Enabled = true, Directory = root, SessionId = "session-test" },
                new ProxyMetrics(),
                "V3_4_3_54261",
                "V3_3_5a_12340");

            telemetry.Record("unknown_opcode", "server_to_client", "SMSG_LOOT_LIST", "wire=0x3F9");
            telemetry.Flush();

            Assert.True(File.Exists(telemetry.EventsPath));
            Assert.True(File.Exists(telemetry.SummaryPath));
            Assert.True(File.Exists(telemetry.TaskQueuePath));
            Assert.Contains("HERMES-WOTLK-LOOT-LIST", File.ReadAllText(telemetry.TaskQueuePath));
            Assert.DoesNotContain("payload", File.ReadAllText(telemetry.EventsPath), StringComparison.OrdinalIgnoreCase);

            using var summary = JsonDocument.Parse(File.ReadAllText(telemetry.SummaryPath));
            Assert.Equal("session-test", summary.RootElement.GetProperty("SessionId").GetString());
            Assert.Equal(1, summary.RootElement.GetProperty("Counters").GetProperty("unknown_opcode.SMSG_LOOT_LIST").GetInt64());
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }
}

namespace HermesProxy.Configuration.Options;

public sealed class TelemetryOptions
{
    public bool Enabled { get; set; } = true;

    public string Directory { get; set; } = "Logs/Telemetry";

    public string SessionId { get; set; } = string.Empty;

    public int FlushIntervalSeconds { get; set; } = 10;

    public int MaxBufferedEvents { get; set; } = 4096;
}

using Framework.Logging;
using HermesProxy.World.Enums;
using System;

namespace HermesProxy.World;

/// <summary>
/// Opt-in diagnostic gate for movement, transport, zoning, and spline wire emission.
/// Set the <c>HERMES_TRACE_MOVEMENT</c> environment variable to any non-empty value before
/// launching HermesProxy to capture per-packet traces in the proxy log. Default off; zero
/// overhead on the hot path when disabled (single static-readonly bool check).
/// </summary>
public static class MovementTrace
{
    public static readonly bool Enabled =
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("HERMES_TRACE_MOVEMENT"));

    public static bool IsMovementOpcode(Opcode opcode)
    {
        var name = opcode.ToString();
        return name.Contains("MOVE", StringComparison.Ordinal) ||
               name.Contains("TRANSFER", StringComparison.Ordinal) ||
               name.Contains("WORLDPORT", StringComparison.Ordinal) ||
               name.Contains("NEW_WORLD", StringComparison.Ordinal) ||
               name.Contains("TIME_SYNC", StringComparison.Ordinal);
    }

    public static void Record(string direction, Opcode opcode, uint wireOpcode, string detail = "")
    {
        if (Enabled && IsMovementOpcode(opcode))
            Log.Print(LogType.Trace, $"[Movement/{direction}] opcode={opcode} wire=0x{wireOpcode:X} {detail}");
    }
}

using Framework.GameMath;
using System;

namespace HermesProxy.World.Objects;

public enum MovementRejection
{
    None,
    DuplicateTimestamp,
    StaleTimestamp,
    NonFiniteValue,
    ImpossibleCoordinate,
    InvalidTransport,
}

// Owns the movement time-base for one game session.  The legacy server receives a
// monotonic clock even though the modern client clock starts independently and wraps.
public sealed class MovementSessionState
{
    private const float MaximumCoordinate = 100000f;
    private const float MaximumTransportOffset = 1000f;

    private bool _hasTimestamp;
    private uint _lastClientTimestamp;
    private uint _legacyTimestamp;
    private long _lastAcceptedAtMs;

    public uint CorrectionCount { get; private set; }
    public long? LastCorrectionDelayMs { get; private set; }

    public bool TryAccept(MovementInfo movement, long receivedAtMs, out MovementRejection rejection)
    {
        lock (this)
        {
            if (!TryValidate(movement, out rejection))
                return false;

            if (!_hasTimestamp)
            {
                _hasTimestamp = true;
                _lastClientTimestamp = movement.MoveTime;
                _legacyTimestamp = 0;
                _lastAcceptedAtMs = receivedAtMs;
                movement.MoveTime = _legacyTimestamp;
                rejection = MovementRejection.None;
                return true;
            }

            uint elapsed = unchecked(movement.MoveTime - _lastClientTimestamp);
            if (elapsed == 0)
            {
                rejection = MovementRejection.DuplicateTimestamp;
                return false;
            }

            if (elapsed > int.MaxValue)
            {
                rejection = MovementRejection.StaleTimestamp;
                return false;
            }

            _lastClientTimestamp = movement.MoveTime;
            _legacyTimestamp = unchecked(_legacyTimestamp + elapsed);
            _lastAcceptedAtMs = receivedAtMs;
            movement.MoveTime = _legacyTimestamp;
            rejection = MovementRejection.None;
            return true;
        }
    }

    public void RecordServerCorrection(long receivedAtMs)
    {
        lock (this)
        {
            if (!_hasTimestamp || receivedAtMs < _lastAcceptedAtMs)
                return;

            CorrectionCount++;
            LastCorrectionDelayMs = receivedAtMs - _lastAcceptedAtMs;
        }
    }

    private static bool TryValidate(MovementInfo movement, out MovementRejection rejection)
    {
        if (!IsFinite(movement.Position) ||
            !IsFinite(movement.Orientation) ||
            !IsFinite(movement.SwimPitch) ||
            !IsFinite(movement.SplineElevation) ||
            !IsFinite(movement.JumpHorizontalSpeed) ||
            !IsFinite(movement.JumpVerticalSpeed) ||
            !IsFinite(movement.JumpCosAngle) ||
            !IsFinite(movement.JumpSinAngle))
        {
            rejection = MovementRejection.NonFiniteValue;
            return false;
        }

        if (MathF.Abs(movement.Position.X) > MaximumCoordinate ||
            MathF.Abs(movement.Position.Y) > MaximumCoordinate ||
            MathF.Abs(movement.Position.Z) > MaximumCoordinate)
        {
            rejection = MovementRejection.ImpossibleCoordinate;
            return false;
        }

        if (movement.TransportGuid != default)
        {
            if (movement.TransportSeat < -1 ||
                !IsFinite(movement.TransportOffset) ||
                !IsFinite(movement.TransportOrientation) ||
                MathF.Abs(movement.TransportOffset.X) > MaximumTransportOffset ||
                MathF.Abs(movement.TransportOffset.Y) > MaximumTransportOffset ||
                MathF.Abs(movement.TransportOffset.Z) > MaximumTransportOffset ||
                !IsForwardOrEqual(movement.TransportTime2, movement.TransportTime))
            {
                rejection = MovementRejection.InvalidTransport;
                return false;
            }
        }

        rejection = MovementRejection.None;
        return true;
    }

    private static bool IsFinite(Vector3 vector) =>
        IsFinite(vector.X) && IsFinite(vector.Y) && IsFinite(vector.Z);

    private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

    private static bool IsForwardOrEqual(uint previous, uint current) =>
        unchecked(current - previous) <= int.MaxValue;
}

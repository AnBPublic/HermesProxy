using Framework.GameMath;
using HermesProxy.World;
using HermesProxy.World.Enums;
using HermesProxy.World.Objects;
using Xunit;

namespace HermesProxy.Tests.World;

public sealed class MovementSessionStateTests
{
    [Fact]
    public void Replay_RebasesTimestampsAndRejectsDuplicateAndStalePackets()
    {
        var state = new MovementSessionState();
        var first = Move(7000);
        var next = Move(7020);

        Assert.True(state.TryAccept(first, 100, out var firstRejection));
        Assert.Equal(MovementRejection.None, firstRejection);
        Assert.Equal(0u, first.MoveTime);
        Assert.True(state.TryAccept(next, 120, out var nextRejection));
        Assert.Equal(MovementRejection.None, nextRejection);
        Assert.Equal(20u, next.MoveTime);
        Assert.False(state.TryAccept(Move(7020), 140, out var duplicateRejection));
        Assert.Equal(MovementRejection.DuplicateTimestamp, duplicateRejection);
        Assert.False(state.TryAccept(Move(7010), 160, out var staleRejection));
        Assert.Equal(MovementRejection.StaleTimestamp, staleRejection);
    }

    [Fact]
    public void Replay_AcceptsTimestampWraparoundAsForwardMovement()
    {
        var state = new MovementSessionState();
        var beforeWrap = Move(uint.MaxValue - 3);
        var afterWrap = Move(2);

        Assert.True(state.TryAccept(beforeWrap, 100, out _));
        Assert.True(state.TryAccept(afterWrap, 106, out var rejection));
        Assert.Equal(MovementRejection.None, rejection);
        Assert.Equal(6u, afterWrap.MoveTime);
    }

    [Fact]
    public void RejectsNonFiniteCoordinatesWithoutAdvancingTheTimeline()
    {
        var state = new MovementSessionState();
        var invalid = Move(500);
        invalid.Position = new Vector3(float.NaN, 0, 0);

        Assert.False(state.TryAccept(invalid, 100, out var rejection));
        Assert.Equal(MovementRejection.NonFiniteValue, rejection);

        var firstValid = Move(500);
        Assert.True(state.TryAccept(firstValid, 120, out _));
        Assert.Equal(0u, firstValid.MoveTime);
    }

    [Fact]
    public void RejectsImpossibleCoordinatesWithoutAdvancingTheTimeline()
    {
        var state = new MovementSessionState();
        var invalid = Move(500);
        invalid.Position = new Vector3(100001, 0, 0);

        Assert.False(state.TryAccept(invalid, 100, out var rejection));
        Assert.Equal(MovementRejection.ImpossibleCoordinate, rejection);

        var firstValid = Move(500);
        Assert.True(state.TryAccept(firstValid, 120, out _));
        Assert.Equal(0u, firstValid.MoveTime);
    }

    [Fact]
    public void RejectsInvalidTransportState()
    {
        var state = new MovementSessionState();
        var invalid = Move(1000);
        invalid.TransportGuid = WowGuid128.Create(HighGuidType703.Transport, 1);
        invalid.TransportSeat = -2;
        invalid.TransportTime = 100;
        invalid.TransportTime2 = 50;

        Assert.False(state.TryAccept(invalid, 100, out var rejection));
        Assert.Equal(MovementRejection.InvalidTransport, rejection);
    }

    [Fact]
    public void RejectsTransportWithReorderedPreviousTime()
    {
        var state = new MovementSessionState();
        var invalid = Move(1000);
        invalid.TransportGuid = WowGuid128.Create(HighGuidType703.Transport, 1);
        invalid.TransportSeat = 0;
        invalid.TransportTime = 100;
        invalid.TransportTime2 = 500;

        Assert.False(state.TryAccept(invalid, 100, out var rejection));
        Assert.Equal(MovementRejection.InvalidTransport, rejection);
    }

    [Fact]
    public void CorrelatesServerCorrectionsWithTheLastAcceptedMovement()
    {
        var state = new MovementSessionState();
        Assert.True(state.TryAccept(Move(100), 500, out _));

        state.RecordServerCorrection(545);

        Assert.Equal(1u, state.CorrectionCount);
        Assert.Equal(45L, state.LastCorrectionDelayMs);
    }

    private static MovementInfo Move(uint time) => new()
    {
        MoveTime = time,
        Position = new Vector3(1, 2, 3),
        Orientation = 1,
    };
}

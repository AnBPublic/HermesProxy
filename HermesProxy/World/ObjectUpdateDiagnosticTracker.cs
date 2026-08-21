using System;
using System.Collections.Generic;
using System.Threading;
using HermesProxy.Enums;
using HermesProxy.World.Enums;

namespace HermesProxy.World;

public sealed record ObjectUpdateDiagnostic(
    long Sequence,
    WowGuid128 Guid,
    string ObjectType,
    string GuidCategory,
    UpdateTypeModern UpdateKind,
    string SerializerSection,
    string FixtureReference);

public sealed class ObjectUpdateDiagnosticTracker
{
    private readonly Lock _lock = new();
    private readonly Dictionary<WowGuid128, ObjectUpdateDiagnostic> _latest = [];
    private long _sequence;

    public void Record(WowGuid128 guid, ObjectType objectType, UpdateTypeModern updateKind,
        string serializerSection, string fixtureReference)
        => Record(guid, objectType.ToString(), updateKind, serializerSection, fixtureReference);

    public void Record(WowGuid128 guid, string objectType, UpdateTypeModern updateKind,
        string serializerSection, string fixtureReference)
    {
        var diagnostic = new ObjectUpdateDiagnostic(
            Interlocked.Increment(ref _sequence), guid, objectType, guid.GetHighType().ToString(),
            updateKind, serializerSection, string.IsNullOrWhiteSpace(fixtureReference) ? "live" : fixtureReference);
        lock (_lock)
            _latest[guid] = diagnostic;
    }

    public void Forget(WowGuid128 guid)
    {
        lock (_lock)
            _latest.Remove(guid);
    }

    public string DescribeFailure(WowGuid128 guid)
    {
        lock (_lock)
        {
            if (_latest.TryGetValue(guid, out var diagnostic))
                return $"objectType={diagnostic.ObjectType} guid={guid} guidCategory={diagnostic.GuidCategory} " +
                       $"updateKind={diagnostic.UpdateKind} serializerSection={diagnostic.SerializerSection} " +
                       $"fixture={diagnostic.FixtureReference} sequence={diagnostic.Sequence}";
        }

        return $"objectType=Unknown guid={guid} guidCategory={guid.GetHighType()} " +
               "updateKind=Unknown serializerSection=Unknown fixture=unmatched";
    }
}

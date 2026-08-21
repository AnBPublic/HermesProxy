using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using HermesProxy.World.Enums;

namespace HermesProxy.World;

public enum ModernOnlyDirection { ClientToServer, ServerToClient }
public enum ModernOnlyDisposition { RequiredTranslation, SafeMinimalResponse, SafeIgnoredNotification, CaptureRequired, UnsupportedFeature }

public sealed record ModernOnlyCompatibilityRecord(Opcode Opcode, ModernOnlyDirection Direction, ModernOnlyDisposition Disposition, string InvestigationId, string Subsystem, string GameplayContext, string Name, string UserMessage);

public static class ModernOnlyCompatibilityMatrix
{
    private static readonly ModernOnlyCompatibilityRecord[] Records =
    [
        new(Opcode.CMSG_BATTLE_PAY_GET_PRODUCT_LIST, ModernOnlyDirection.ClientToServer, ModernOnlyDisposition.SafeMinimalResponse, "MODERN-SVC-001", "battle-pay", "login", "CMSG_BATTLE_PAY_GET_PRODUCT_LIST", "Battle Pay is unavailable on this legacy realm."),
        new(Opcode.CMSG_BATTLE_PAY_GET_PURCHASE_LIST, ModernOnlyDirection.ClientToServer, ModernOnlyDisposition.SafeMinimalResponse, "MODERN-SVC-002", "battle-pay", "login", "CMSG_BATTLE_PAY_GET_PURCHASE_LIST", "Battle Pay is unavailable on this legacy realm."),
        new(Opcode.CMSG_UPDATE_VAS_PURCHASE_STATES, ModernOnlyDirection.ClientToServer, ModernOnlyDisposition.SafeMinimalResponse, "MODERN-SVC-003", "vas", "login", "CMSG_UPDATE_VAS_PURCHASE_STATES", "Character services are unavailable on this legacy realm."),
        new(Opcode.CMSG_GET_UNDELETE_CHARACTER_COOLDOWN_STATUS, ModernOnlyDirection.ClientToServer, ModernOnlyDisposition.SafeMinimalResponse, "MODERN-SVC-004", "character-services", "character-select", "CMSG_GET_UNDELETE_CHARACTER_COOLDOWN_STATUS", "Character undelete is unavailable on this legacy realm."),
        new(Opcode.CMSG_REQUEST_PVP_REWARDS, ModernOnlyDirection.ClientToServer, ModernOnlyDisposition.CaptureRequired, "MODERN-SVC-005", "pvp", "in-world", "CMSG_REQUEST_PVP_REWARDS", "PvP reward compatibility requires a packet capture before it can be enabled."),
        new(Opcode.CMSG_REQUEST_FORCED_REACTIONS, ModernOnlyDirection.ClientToServer, ModernOnlyDisposition.RequiredTranslation, "MODERN-SVC-006", "reputation", "in-world", "CMSG_REQUEST_FORCED_REACTIONS", "Forced-reaction compatibility requires translation validation."),
        new(Opcode.SMSG_SET_FORCED_REACTIONS, ModernOnlyDirection.ServerToClient, ModernOnlyDisposition.RequiredTranslation, "MODERN-SVC-007", "reputation", "in-world", "SMSG_SET_FORCED_REACTIONS", "Forced-reaction compatibility requires translation validation."),
        new(Opcode.CMSG_CALENDAR_GET_NUM_PENDING, ModernOnlyDirection.ClientToServer, ModernOnlyDisposition.SafeMinimalResponse, "MODERN-SVC-008", "calendar", "login", "CMSG_CALENDAR_GET_NUM_PENDING", "Calendar notifications are unavailable on this legacy realm."),
        new(Opcode.CMSG_QUERY_COUNTDOWN_TIMER, ModernOnlyDirection.ClientToServer, ModernOnlyDisposition.SafeMinimalResponse, "MODERN-SVC-009", "world-ui", "in-world", "CMSG_QUERY_COUNTDOWN_TIMER", "This countdown service is unavailable on this legacy realm."),
        new(Opcode.CMSG_BATTLE_PET_REQUEST_JOURNAL, ModernOnlyDirection.ClientToServer, ModernOnlyDisposition.SafeMinimalResponse, "MODERN-SVC-010", "battle-pets", "login", "CMSG_BATTLE_PET_REQUEST_JOURNAL", "Battle pets are unavailable on this legacy realm."),
        new(Opcode.CMSG_GM_TICKET_GET_SYSTEM_STATUS, ModernOnlyDirection.ClientToServer, ModernOnlyDisposition.SafeMinimalResponse, "MODERN-SVC-011", "support", "login", "CMSG_GM_TICKET_GET_SYSTEM_STATUS", "In-client GM tickets are unavailable on this legacy realm."),
        new(Opcode.CMSG_GM_TICKET_GET_CASE_STATUS, ModernOnlyDirection.ClientToServer, ModernOnlyDisposition.SafeMinimalResponse, "MODERN-SVC-012", "support", "login", "CMSG_GM_TICKET_GET_CASE_STATUS", "In-client GM tickets are unavailable on this legacy realm."),
        new(Opcode.CMSG_REPORT_CLIENT_VARIABLES, ModernOnlyDirection.ClientToServer, ModernOnlyDisposition.SafeIgnoredNotification, "MODERN-SVC-013", "client-reporting", "login", "CMSG_REPORT_CLIENT_VARIABLES", ""),
        new(Opcode.CMSG_REPORT_ENABLED_ADDONS, ModernOnlyDirection.ClientToServer, ModernOnlyDisposition.SafeIgnoredNotification, "MODERN-SVC-014", "client-reporting", "login", "CMSG_REPORT_ENABLED_ADDONS", ""),
        new(Opcode.CMSG_REPORT_KEYBINDING_EXECUTION_COUNTS, ModernOnlyDirection.ClientToServer, ModernOnlyDisposition.SafeIgnoredNotification, "MODERN-SVC-015", "client-reporting", "login", "CMSG_REPORT_KEYBINDING_EXECUTION_COUNTS", ""),
        new(Opcode.SMSG_PLAY_SPELL_IMPACT, ModernOnlyDirection.ServerToClient, ModernOnlyDisposition.CaptureRequired, "P4-SPELL-IMPACT", "spell-visual", "in-world", "SMSG_PLAY_SPELL_IMPACT", "Spell-impact translation is blocked until a native 3.4.3.54261 capture establishes the wire layout."),
        new(Opcode.SMSG_TRAINER_BUY_SUCCEEDED, ModernOnlyDirection.ServerToClient, ModernOnlyDisposition.CaptureRequired, "P4-TRAINER-BUY-SUCCEEDED", "trainer", "in-world", "SMSG_TRAINER_BUY_SUCCEEDED", "Trainer-buy completion translation is blocked until a native 3.4.3.54261 capture establishes the wire layout."),
        new(Opcode.SMSG_LOAD_EQUIPMENT_SET, ModernOnlyDirection.ServerToClient, ModernOnlyDisposition.CaptureRequired, "P4-LOAD-EQUIPMENT-SET", "equipment-set", "in-world", "SMSG_LOAD_EQUIPMENT_SET", "Equipment-set translation is blocked until a native 3.4.3.54261 capture establishes the wire layout."),
        new(Opcode.SMSG_INSTANCE_DIFFICULTY, ModernOnlyDirection.ServerToClient, ModernOnlyDisposition.CaptureRequired, "P4-INSTANCE-DIFFICULTY", "instance", "in-world", "SMSG_INSTANCE_DIFFICULTY", "Instance-difficulty translation is blocked until a native 3.4.3.54261 capture establishes the wire layout."),
    ];

    private static readonly Dictionary<(Opcode, ModernOnlyDirection), ModernOnlyCompatibilityRecord> ByOpcode = Records.ToDictionary(record => (record.Opcode, record.Direction));
    private static readonly Dictionary<uint, ModernOnlyCompatibilityRecord> LegacyServerWireRecords = new()
    {
        [0x01F7] = ByOpcode[(Opcode.SMSG_PLAY_SPELL_IMPACT, ModernOnlyDirection.ServerToClient)],
        [0x01B3] = ByOpcode[(Opcode.SMSG_TRAINER_BUY_SUCCEEDED, ModernOnlyDirection.ServerToClient)],
        [0x04BC] = ByOpcode[(Opcode.SMSG_LOAD_EQUIPMENT_SET, ModernOnlyDirection.ServerToClient)],
        [0x033B] = ByOpcode[(Opcode.SMSG_INSTANCE_DIFFICULTY, ModernOnlyDirection.ServerToClient)],
    };
    private static readonly ConcurrentDictionary<string, long> Counters = new();
    public static IReadOnlyList<ModernOnlyCompatibilityRecord> StartupLoginBaseline { get; } = Records.Where(record => record.GameplayContext is "login" or "character-select").ToArray();
    public static bool TryGet(Opcode opcode, ModernOnlyDirection direction, out ModernOnlyCompatibilityRecord record) => ByOpcode.TryGetValue((opcode, direction), out record!);
    public static ModernOnlyCompatibilityRecord DescribeUnmappedWireOpcode(ModernOnlyDirection direction, uint wireOpcode)
    {
        if (direction == ModernOnlyDirection.ServerToClient && LegacyServerWireRecords.TryGetValue(wireOpcode, out var record))
            return record;

        return new(Opcode.MSG_NULL_ACTION, direction, ModernOnlyDisposition.CaptureRequired, $"MODERN-WIRE-{(direction == ModernOnlyDirection.ClientToServer ? "C2S" : "S2C")}-0x{wireOpcode:X}", "unclassified", "unknown", $"UNKNOWN_0x{wireOpcode:X}", "An unclassified modern client service packet was blocked.");
    }
    public static bool Record(ModernOnlyCompatibilityRecord record)
    {
        var count = Counters.AddOrUpdate(record.InvestigationId, 1, static (_, current) => current + 1);
        HermesProxy.Server.Telemetry?.Record("modern_only_opcode", record.Direction.ToString(), record.Name, $"id={record.InvestigationId};class={record.Disposition};subsystem={record.Subsystem};context={record.GameplayContext};count={count}");
        return count == 1 || count % 100 == 0;
    }
}

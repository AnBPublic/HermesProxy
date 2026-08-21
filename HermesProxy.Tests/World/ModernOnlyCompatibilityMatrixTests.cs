using System.Linq;
using HermesProxy.World;
using HermesProxy.World.Enums;
using Xunit;

namespace HermesProxy.Tests.World;

public sealed class ModernOnlyCompatibilityMatrixTests
{
    [Fact]
    public void StartupLoginBaselineIsFullyClassifiedAndStable()
    {
        var baseline = ModernOnlyCompatibilityMatrix.StartupLoginBaseline;
        Assert.Equal(new[] { Opcode.CMSG_BATTLE_PAY_GET_PRODUCT_LIST, Opcode.CMSG_BATTLE_PAY_GET_PURCHASE_LIST, Opcode.CMSG_UPDATE_VAS_PURCHASE_STATES, Opcode.CMSG_GET_UNDELETE_CHARACTER_COOLDOWN_STATUS, Opcode.CMSG_CALENDAR_GET_NUM_PENDING, Opcode.CMSG_BATTLE_PET_REQUEST_JOURNAL, Opcode.CMSG_GM_TICKET_GET_SYSTEM_STATUS, Opcode.CMSG_GM_TICKET_GET_CASE_STATUS, Opcode.CMSG_REPORT_CLIENT_VARIABLES, Opcode.CMSG_REPORT_ENABLED_ADDONS, Opcode.CMSG_REPORT_KEYBINDING_EXECUTION_COUNTS }, baseline.Select(entry => entry.Opcode));
        Assert.All(baseline, entry => Assert.True(ModernOnlyCompatibilityMatrix.TryGet(entry.Opcode, ModernOnlyDirection.ClientToServer, out _), entry.Opcode.ToString()));
    }

    [Theory]
    [InlineData(Opcode.CMSG_BATTLE_PAY_GET_PRODUCT_LIST, ModernOnlyDisposition.SafeMinimalResponse, "battle-pay")]
    [InlineData(Opcode.CMSG_UPDATE_VAS_PURCHASE_STATES, ModernOnlyDisposition.SafeMinimalResponse, "vas")]
    [InlineData(Opcode.CMSG_GET_UNDELETE_CHARACTER_COOLDOWN_STATUS, ModernOnlyDisposition.SafeMinimalResponse, "character-services")]
    [InlineData(Opcode.CMSG_REQUEST_PVP_REWARDS, ModernOnlyDisposition.CaptureRequired, "pvp")]
    [InlineData(Opcode.CMSG_REQUEST_FORCED_REACTIONS, ModernOnlyDisposition.RequiredTranslation, "reputation")]
    [InlineData(Opcode.CMSG_CALENDAR_GET_NUM_PENDING, ModernOnlyDisposition.SafeMinimalResponse, "calendar")]
    [InlineData(Opcode.CMSG_BATTLE_PET_REQUEST_JOURNAL, ModernOnlyDisposition.SafeMinimalResponse, "battle-pets")]
    [InlineData(Opcode.CMSG_GM_TICKET_GET_CASE_STATUS, ModernOnlyDisposition.SafeMinimalResponse, "support")]
    [InlineData(Opcode.CMSG_REPORT_CLIENT_VARIABLES, ModernOnlyDisposition.SafeIgnoredNotification, "client-reporting")]
    public void RecurringModernServiceTrafficHasAnExplicitDisposition(Opcode opcode, ModernOnlyDisposition disposition, string subsystem)
    {
        Assert.True(ModernOnlyCompatibilityMatrix.TryGet(opcode, ModernOnlyDirection.ClientToServer, out var record));
        Assert.Equal(disposition, record.Disposition); Assert.Equal(subsystem, record.Subsystem); Assert.False(string.IsNullOrWhiteSpace(record.InvestigationId));
    }

    [Fact]
    public void UnmappedWireOpcodeUsesANamedInvestigationRecord()
    {
        var record = ModernOnlyCompatibilityMatrix.DescribeUnmappedWireOpcode(ModernOnlyDirection.ClientToServer, 0xDEADu);
        Assert.Equal("MODERN-WIRE-C2S-0xDEAD", record.InvestigationId); Assert.Equal("UNKNOWN_0xDEAD", record.Name); Assert.Equal(ModernOnlyDisposition.CaptureRequired, record.Disposition);
    }

    [Fact]
    public void RepeatedDiagnosticsAreRateLimitedWhileCountersContinue()
    {
        var record = ModernOnlyCompatibilityMatrix.DescribeUnmappedWireOpcode(ModernOnlyDirection.ClientToServer, 0xC0DEF00Du);

        Assert.True(ModernOnlyCompatibilityMatrix.Record(record));
        Assert.False(ModernOnlyCompatibilityMatrix.Record(record));
    }

    [Theory]
    [InlineData(Opcode.SMSG_PLAY_SPELL_IMPACT, "P4-SPELL-IMPACT")]
    [InlineData(Opcode.SMSG_TRAINER_BUY_SUCCEEDED, "P4-TRAINER-BUY-SUCCEEDED")]
    [InlineData(Opcode.SMSG_LOAD_EQUIPMENT_SET, "P4-LOAD-EQUIPMENT-SET")]
    [InlineData(Opcode.SMSG_INSTANCE_DIFFICULTY, "P4-INSTANCE-DIFFICULTY")]
    public void Patch4PacketsWithoutACapturedModernLayoutAreExplicitlyBlocked(Opcode opcode, string investigationId)
    {
        Assert.True(ModernOnlyCompatibilityMatrix.TryGet(opcode, ModernOnlyDirection.ServerToClient, out var record));
        Assert.Equal(ModernOnlyDisposition.CaptureRequired, record.Disposition);
        Assert.Equal(investigationId, record.InvestigationId);
    }

    [Theory]
    [InlineData(0x01F7u, Opcode.SMSG_PLAY_SPELL_IMPACT, "P4-SPELL-IMPACT")]
    [InlineData(0x01B3u, Opcode.SMSG_TRAINER_BUY_SUCCEEDED, "P4-TRAINER-BUY-SUCCEEDED")]
    [InlineData(0x04BCu, Opcode.SMSG_LOAD_EQUIPMENT_SET, "P4-LOAD-EQUIPMENT-SET")]
    [InlineData(0x033Bu, Opcode.SMSG_INSTANCE_DIFFICULTY, "P4-INSTANCE-DIFFICULTY")]
    public void Patch4LegacyWireOpcodesProduceNamedCaptureRequiredDecisions(uint wireOpcode, Opcode opcode, string investigationId)
    {
        var record = ModernOnlyCompatibilityMatrix.DescribeUnmappedWireOpcode(ModernOnlyDirection.ServerToClient, wireOpcode);

        Assert.Equal(opcode, record.Opcode);
        Assert.Equal(investigationId, record.InvestigationId);
        Assert.Equal(ModernOnlyDisposition.CaptureRequired, record.Disposition);
    }
}

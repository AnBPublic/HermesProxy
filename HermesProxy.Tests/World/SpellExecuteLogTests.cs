using System.Linq;
using System.Reflection;
using HermesProxy.World;
using HermesProxy.World.Client;
using HermesProxy.World.Enums;
using Xunit;

namespace HermesProxy.Tests.World;

public sealed class SpellExecuteLogTests
{
    [Fact]
    public void Wotlk343MapsSpellExecuteLogToItsModernWireOpcode()
    {
        Assert.Equal(0x2C40u, (uint)HermesProxy.World.Enums.V3_4_3_54261.Opcode.SMSG_SPELL_EXECUTE_LOG);
    }

    [Fact]
    public void SpellExecuteLogHandlerIsRegistered()
    {
        var method = typeof(WorldClient).GetMethod("HandleSpellExecuteLog", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);

        var opcodes = method!.GetCustomAttributes<PacketHandlerAttribute>()
            .Select(attribute => attribute.Opcode)
            .ToHashSet();

        Assert.Contains(Opcode.SMSG_SPELL_EXECUTE_LOG, opcodes);
    }
}

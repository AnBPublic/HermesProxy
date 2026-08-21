using System;
using System.Linq;
using System.Reflection;
using HermesProxy.World;
using HermesProxy.World.Enums;
using HermesProxy.World.Server;
using HermesProxy.World.Server.Packets;
using Xunit;

namespace HermesProxy.Tests.World;

public sealed class CloseInteractionTests
{
    [Fact]
    public void CloseInteraction_ReadsModernPackedSourceGuid()
    {
        var expected = new WowGuid128(0x1122334455667788, 0x8877665544332211);
        using var encoded = new WorldPacket();
        encoded.WritePackedGuid128(expected);
        var data = (byte[])encoded.GetData().Clone();

        var packet = new WorldPacket(0x1234, data);
        using var closeInteraction = new CloseInteraction(packet);

        closeInteraction.Read();

        Assert.Equal(expected, closeInteraction.SourceGuid);
        Assert.False(packet.CanRead());
    }

    [Fact]
    public void CloseInteraction_HandlerIsRegisteredForModernOpcode()
    {
        var method = typeof(WorldSocket).GetMethod("HandleCloseInteraction", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        Assert.Equal(typeof(CloseInteraction), method!.GetParameters().Single().ParameterType);

        var opcodes = method.GetCustomAttributes<PacketHandlerAttribute>()
            .Select(attribute => attribute.Opcode)
            .ToHashSet();

        Assert.Contains(Opcode.CMSG_CLOSE_INTERACTION, opcodes);
    }
}

using System;
using HermesProxy.World;
using Xunit;

namespace HermesProxy.Tests.World;

public sealed class BinderCompatibilityTests
{
    [Fact]
    public void Wrath343BinderResultUsesNpcInteractionOpenResultWirePacket()
    {
        Type? packetType = typeof(ServerPacket).Assembly.GetType(
            "HermesProxy.World.Server.Packets.BinderInteractionOpenResult343");

        Assert.NotNull(packetType);

        var packet = (ServerPacket)Activator.CreateInstance(packetType!)!;

        Assert.Equal(0x288Au, packet.GetOpcode());

        packet.WritePacketData();

        Assert.Equal(new byte[] { 0, 0, 20, 0, 0, 0, 0x80 }, packet.GetData());
    }
}

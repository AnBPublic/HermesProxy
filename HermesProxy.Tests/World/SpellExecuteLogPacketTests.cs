using System;
using HermesProxy.World;
using HermesProxy.World.Server.Packets;
using Xunit;

namespace HermesProxy.Tests.World;

public sealed class SpellExecuteLogPacketTests
{
    [Fact]
    public void ModernEffectWritesTheRequiredSingleLogCount()
    {
        using var packet = new WorldPacket();
        new SpellExecuteLogEffect { Effect = (uint)SpellExecuteLogEffectType.CreateItem, Entry = 1234 }.Write(packet);

        var data = packet.GetData();
        Assert.Equal(12, data.Length);
        Assert.Equal(24u, BitConverter.ToUInt32(data, 0));
        Assert.Equal(1u, BitConverter.ToUInt32(data, 4));
        Assert.Equal(1234u, BitConverter.ToUInt32(data, 8));
    }
}

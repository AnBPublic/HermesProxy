using System;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using HermesProxy;
using HermesProxy.Configuration.Options;
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
    public void CloseInteraction_HandlerIsRegisteredByTheProductionSocket()
    {
        VersionBootstrap.LegacyBuild = global::HermesProxy.Enums.ClientVersionBuild.V3_3_5a_12340;
        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        using var worldSocket = new WorldSocket(socket, Microsoft.Extensions.Options.Options.Create(new ProxyNetworkOptions()));

        Assert.NotNull(worldSocket.GetHandler(Opcode.CMSG_CLOSE_INTERACTION));
    }

    [Fact]
    public void CloseInteraction_ConsumesTheActiveInteractionOnlyOnce()
    {
        VersionBootstrap.LegacyBuild = global::HermesProxy.Enums.ClientVersionBuild.V3_3_5a_12340;
        var global = (GlobalSessionData)RuntimeHelpers.GetUninitializedObject(typeof(GlobalSessionData));
        var state = GameSessionData.CreateNewGameSessionData(global);
        var source = new WowGuid128(0x1122334455667788, 0x8877665544332211);
        state.CurrentInteractedWithNPC = source;

        Assert.True(CloseInteraction.TryBuildLegacyCancel(state, source, out var cancel));
        Assert.Equal(Opcode.CMSG_QUEST_GIVER_CANCEL, cancel.GetUniversalOpcode(false));
        Assert.True(state.CurrentInteractedWithNPC.IsEmpty());

        Assert.False(CloseInteraction.TryBuildLegacyCancel(state, source, out _));
    }
}

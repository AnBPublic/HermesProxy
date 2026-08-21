using System;
using System.Linq;
using System.Reflection;
using HermesProxy.World;
using HermesProxy.World.Server;
using Xunit;

namespace HermesProxy.Tests.World;

public sealed class TalentPreviewCompatibilityTests
{
    [Fact]
    public void Wrath343PreviewCommitTranslatesBatchToLegacyPreviewPacket()
    {
        using var payload = new WorldPacket();
        payload.WriteUInt32(1); // modern TabPage, absent from the legacy packet
        payload.WriteUInt32(2);
        payload.WriteUInt32(124); payload.WriteUInt32(2);
        payload.WriteUInt32(130); payload.WriteUInt32(4);

        using var modern = new WorldPacket(0x3553u, (byte[])payload.GetData().Clone());
        using var preview = new LearnPreviewTalents343(modern);
        preview.Read();

        Assert.True(preview.IsValid);
        Assert.Equal(2, preview.Talents.Count);

        using var legacy = preview.BuildLegacyPacket();

        Assert.Equal(0x4C1u, legacy.GetOpcode());
        Assert.Equal(new byte[]
        {
            2, 0, 0, 0,
            124, 0, 0, 0, 2, 0, 0, 0,
            130, 0, 0, 0, 4, 0, 0, 0
        }, legacy.GetData());
    }

    [Fact]
    public void Wrath343PreviewOpcodeHasDedicatedInternalDispatchHandler()
    {
        Assert.True(WorldPacket.IsWotLK343LearnPreviewTalents(0x3553u, 0x3552u));
        Assert.False(WorldPacket.IsWotLK343LearnPreviewTalents(0x3553u, 0x3551u));
        Assert.False(WorldPacket.IsWotLK343LearnPreviewTalents(0x3554u, 0x3552u));

        MethodInfo? handler = typeof(WorldSocket).GetMethod(
            "HandleLearnPreviewTalents343", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(handler);
        Assert.Contains(handler!.GetCustomAttributes<PacketHandlerAttribute>(),
            attribute => (uint)attribute.Opcode == WorldPacket.WotLK343LearnPreviewTalentsDispatch);
    }
}

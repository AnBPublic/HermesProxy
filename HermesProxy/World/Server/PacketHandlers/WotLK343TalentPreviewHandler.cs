using System.Collections.Generic;
using HermesProxy.World;

namespace HermesProxy.World.Server;

/// <summary>
/// Wrath Classic 3.4.3 sends player talent previews as:
/// TabPage, Count, then Count x (TalentID, Rank).
/// Legacy WotLK 3.3.5a expects Count followed by the same talent/rank pairs.
/// </summary>
internal sealed class LearnPreviewTalents343 : ClientPacket
{
    public LearnPreviewTalents343(WorldPacket packet) : base(packet) { }

    public uint TabPage { get; private set; }
    public List<PreviewTalent343> Talents { get; } = new();
    public bool IsValid { get; private set; }

    public override void Read()
    {
        TabPage = _worldPacket.ReadUInt32();
        uint count = _worldPacket.ReadUInt32();

        if (count > 256)
            return;

        for (uint i = 0; i < count; ++i)
        {
            Talents.Add(new PreviewTalent343(
                _worldPacket.ReadUInt32(),
                _worldPacket.ReadUInt32()));
        }

        IsValid = true;
    }

    internal WorldPacket BuildLegacyPacket()
    {
        // 3.3.5a CMSG_LEARN_PREVIEW_TALENTS = 0x4C1. This fork's universal Opcode enum does not
        // contain that legacy-only symbol, so use the verified wire opcode directly.
        WorldPacket packet = new(0x4C1u);
        packet.WriteUInt32((uint)Talents.Count);

        foreach (PreviewTalent343 talent in Talents)
        {
            packet.WriteUInt32(talent.TalentID);
            packet.WriteUInt32(talent.Rank);
        }

        return packet;
    }
}

internal readonly record struct PreviewTalent343(uint TalentID, uint Rank);

public partial class WorldSocket
{
    [PacketHandler(WorldPacket.WotLK343LearnPreviewTalentsDispatch)]
    void HandleLearnPreviewTalents343(LearnPreviewTalents343 preview)
    {
        if (!preview.IsValid)
            return;

        SendPacketToServer(preview.BuildLegacyPacket());
    }
}

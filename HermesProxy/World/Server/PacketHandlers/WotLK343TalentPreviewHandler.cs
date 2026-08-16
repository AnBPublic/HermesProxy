using System.Collections.Generic;
using HermesProxy.World.Enums;

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

    public override void Read()
    {
        TabPage = _worldPacket.ReadUInt32();
        uint count = _worldPacket.ReadUInt32();

        // A normal WotLK talent preview cannot contain an unbounded number of entries.
        // Reject obviously malformed input rather than allocating from an arbitrary client count.
        if (count > 256)
            return;

        for (uint i = 0; i < count; ++i)
        {
            Talents.Add(new PreviewTalent343(
                _worldPacket.ReadUInt32(),
                _worldPacket.ReadUInt32()));
        }
    }
}

internal readonly record struct PreviewTalent343(uint TalentID, uint Rank);

public partial class WorldSocket
{
    [PacketHandler(Opcode.CMSG_LEARN_PREVIEW_TALENTS)]
    void HandleLearnPreviewTalents343(LearnPreviewTalents343 preview)
    {
        WorldPacket packet = new WorldPacket(Opcode.CMSG_LEARN_PREVIEW_TALENTS);
        packet.WriteUInt32((uint)preview.Talents.Count);

        foreach (PreviewTalent343 talent in preview.Talents)
        {
            packet.WriteUInt32(talent.TalentID);
            packet.WriteUInt32(talent.Rank);
        }

        SendPacketToServer(packet);
    }
}

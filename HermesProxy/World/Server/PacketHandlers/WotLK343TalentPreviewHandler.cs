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

        // A WotLK tree cannot legitimately submit hundreds of changes in one preview commit.
        // Reject an obviously malformed count instead of allocating from arbitrary client input.
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
}

internal readonly record struct PreviewTalent343(uint TalentID, uint Rank);

public partial class WorldSocket
{
    // Internal dispatch value produced by WorldPacket.GetUniversalOpcode for raw 3.4.3 opcode 0x3553.
    [PacketHandler(WorldPacket.WotLK343LearnPreviewTalentsDispatch)]
    void HandleLearnPreviewTalents343(LearnPreviewTalents343 preview)
    {
        if (!preview.IsValid)
            return;

        // 3.3.5a CMSG_LEARN_PREVIEW_TALENTS = 0x4C1. Use the verified legacy wire opcode
        // directly because this fork's universal Opcode enum does not contain the preview symbol.
        WorldPacket packet = new WorldPacket(0x4C1u);
        packet.WriteUInt32((uint)preview.Talents.Count);

        foreach (PreviewTalent343 talent in preview.Talents)
        {
            packet.WriteUInt32(talent.TalentID);
            packet.WriteUInt32(talent.Rank);
        }

        SendPacketToServer(packet);
    }
}

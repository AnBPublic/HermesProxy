using HermesProxy.World.Enums;

namespace HermesProxy.World.Server.Packets;

/// <summary>
/// Wrath Classic 3.4.3 no longer exposes SMSG_BINDER_CONFIRM. The client opens the
/// binder confirmation through SMSG_NPC_INTERACTION_OPEN_RESULT instead.
///
/// The universal opcode table in this Hermes fork predates this packet, so the exact
/// 3.4.3.54261 wire opcode is used here. The base packet is initialized with a known
/// valid opcode first, then replaced with the verified 3.4.3 packet opcode.
/// </summary>
public sealed class BinderInteractionOpenResult343 : ServerPacket
{
    private const uint SmsG_NpcInteractionOpenResult_343_54261 = 0x288A;
    private const int BinderInteractionType = 20;

    public BinderInteractionOpenResult343() : base(Opcode.SMSG_GOSSIP_COMPLETE)
    {
        _worldPacket = new WorldPacket(SmsG_NpcInteractionOpenResult_343_54261);
    }

    public WowGuid128 Guid { get; set; }
    public bool Success { get; set; } = true;

    public override void Write()
    {
        _worldPacket.WritePackedGuid128(Guid);
        _worldPacket.WriteInt32(BinderInteractionType);
        _worldPacket.WriteBit(Success);
        _worldPacket.FlushBits();
    }
}

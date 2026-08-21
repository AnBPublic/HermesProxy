using System.Collections.Generic;
using Framework.Constants;
using HermesProxy.World.Enums;

namespace HermesProxy.World.Server.Packets;

public enum SpellExecuteLogEffectType : uint
{
    PowerDrain = 8,
    AddExtraAttacks = 19,
    CreateItem = 24,
    Summon = 28,
    OpenLock = 33,
    TransDoor = 50,
    SummonPet = 56,
    OpenLockItem = 59,
    InterruptCast = 68,
    SummonObjectWild = 76,
    CreateHouse = 81,
    Duel = 83,
    FeedPet = 101,
    DismissPet = 102,
    SummonObjectSlot1 = 104,
    SummonObjectSlot2 = 105,
    SummonObjectSlot3 = 106,
    SummonObjectSlot4 = 107,
    DurabilityDamage = 111,
}

public sealed class SpellExecuteLog : ServerPacket
{
    public SpellExecuteLog() : base(Opcode.SMSG_SPELL_EXECUTE_LOG, ConnectionType.Instance) { }

    public override void Write()
    {
        _worldPacket.WritePackedGuid128(CasterGUID);
        _worldPacket.WriteUInt32(SpellID);
        _worldPacket.WriteUInt32((uint)Effects.Count);

        foreach (var effect in Effects)
            effect.Write(_worldPacket);
    }

    public WowGuid128 CasterGUID;
    public uint SpellID;
    public List<SpellExecuteLogEffect> Effects { get; } = [];
}

public sealed class SpellExecuteLogEffect
{
    public uint Effect;
    public WowGuid128 TargetGUID;
    public uint Amount;
    public uint PowerType;
    public float GainMultiplier;
    public uint Count;
    public uint SpellID;
    public int ItemID;
    public int Slot;
    public uint Entry;

    public void Write(WorldPacket packet)
    {
        packet.WriteUInt32(Effect);
        packet.WriteUInt32(1); // modern SpellLog.amount_of_logs is always one

        switch ((SpellExecuteLogEffectType)Effect)
        {
            case SpellExecuteLogEffectType.PowerDrain:
                packet.WritePackedGuid128(TargetGUID);
                packet.WriteUInt32(Amount);
                packet.WriteUInt32(PowerType);
                packet.WriteFloat(GainMultiplier);
                break;
            case SpellExecuteLogEffectType.AddExtraAttacks:
                packet.WritePackedGuid128(TargetGUID);
                packet.WriteUInt32(Count);
                break;
            case SpellExecuteLogEffectType.InterruptCast:
                packet.WritePackedGuid128(TargetGUID);
                packet.WriteUInt32(SpellID);
                break;
            case SpellExecuteLogEffectType.DurabilityDamage:
                packet.WritePackedGuid128(TargetGUID);
                packet.WriteInt32(ItemID);
                packet.WriteInt32(Slot);
                break;
            case SpellExecuteLogEffectType.OpenLock:
            case SpellExecuteLogEffectType.OpenLockItem:
                packet.WritePackedGuid128(TargetGUID);
                break;
            case SpellExecuteLogEffectType.CreateItem:
                packet.WriteUInt32(Entry);
                break;
            case SpellExecuteLogEffectType.Summon:
            case SpellExecuteLogEffectType.TransDoor:
            case SpellExecuteLogEffectType.SummonPet:
            case SpellExecuteLogEffectType.SummonObjectWild:
            case SpellExecuteLogEffectType.CreateHouse:
            case SpellExecuteLogEffectType.Duel:
            case SpellExecuteLogEffectType.SummonObjectSlot1:
            case SpellExecuteLogEffectType.SummonObjectSlot2:
            case SpellExecuteLogEffectType.SummonObjectSlot3:
            case SpellExecuteLogEffectType.SummonObjectSlot4:
            case SpellExecuteLogEffectType.FeedPet:
            case SpellExecuteLogEffectType.DismissPet:
                packet.WritePackedGuid128(TargetGUID);
                break;
        }
    }
}

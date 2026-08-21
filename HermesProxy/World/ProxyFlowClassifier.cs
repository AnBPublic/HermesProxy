using HermesProxy.World.Enums;

namespace HermesProxy.World;

public enum ProxyFlow
{
    Selection,
    Loot,
    LootItem,
    LootResponse,
    LootList,
    Interaction,
    ObjectUpdate,
    ItemQuery
}

internal static class ProxyFlowClassifier
{
    public static ProxyFlow? ForClientRequest(Opcode opcode) => opcode switch
    {
        Opcode.CMSG_SET_SELECTION => ProxyFlow.Selection,
        Opcode.CMSG_LOOT_UNIT => ProxyFlow.Loot,
        Opcode.CMSG_LOOT_ITEM => ProxyFlow.LootItem,
        Opcode.CMSG_BANKER_ACTIVATE or
        Opcode.CMSG_BINDER_ACTIVATE or
        Opcode.CMSG_LIST_INVENTORY or
        Opcode.CMSG_SPELL_CLICK or
        Opcode.CMSG_SPIRIT_HEALER_ACTIVATE or
        Opcode.CMSG_TALK_TO_GOSSIP or
        Opcode.CMSG_TRAINER_LIST or
        Opcode.CMSG_BATTLEMASTER_HELLO or
        Opcode.CMSG_AREA_SPIRIT_HEALER_QUERY or
        Opcode.CMSG_AREA_SPIRIT_HEALER_QUEUE or
        Opcode.CMSG_GOSSIP_SELECT_OPTION => ProxyFlow.Interaction,
        _ => null
    };

    public static ProxyFlow? ForLegacyResponse(Opcode opcode) => opcode switch
    {
        Opcode.SMSG_LOOT_RESPONSE => ProxyFlow.LootResponse,
        Opcode.SMSG_LOOT_LIST or Opcode.SMSG_LOOT_MASTER_LIST => ProxyFlow.LootList,
        Opcode.SMSG_LOOT_REMOVED => ProxyFlow.LootItem,
        Opcode.SMSG_GOSSIP_MESSAGE or
        Opcode.SMSG_BINDER_CONFIRM or
        Opcode.SMSG_SHOW_BANK or
        Opcode.SMSG_TRAINER_LIST or
        Opcode.SMSG_SPIRIT_HEALER_CONFIRM => ProxyFlow.Interaction,
        Opcode.SMSG_UPDATE_OBJECT => ProxyFlow.ObjectUpdate,
        _ => null
    };
}

using HermesProxy.World.Enums;

namespace HermesProxy.World;

public enum ProxyFlow
{
    Selection,
    Loot,
    LootItem,
    LootResponse,
    LootList,
    ObjectUpdate
}

internal static class ProxyFlowClassifier
{
    public static ProxyFlow? ForClientRequest(Opcode opcode) => opcode switch
    {
        Opcode.CMSG_SET_SELECTION => ProxyFlow.Selection,
        Opcode.CMSG_LOOT_UNIT => ProxyFlow.Loot,
        Opcode.CMSG_LOOT_ITEM => ProxyFlow.LootItem,
        _ => null
    };

    public static ProxyFlow? ForLegacyResponse(Opcode opcode) => opcode switch
    {
        Opcode.SMSG_LOOT_RESPONSE => ProxyFlow.LootResponse,
        Opcode.SMSG_LOOT_LIST or Opcode.SMSG_LOOT_MASTER_LIST => ProxyFlow.LootList,
        Opcode.SMSG_LOOT_REMOVED => ProxyFlow.LootItem,
        Opcode.SMSG_UPDATE_OBJECT => ProxyFlow.ObjectUpdate,
        _ => null
    };
}

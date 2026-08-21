using HermesProxy.World;
using HermesProxy.World.Enums;
using Xunit;

namespace HermesProxy.Tests.World;

public class ProxyFlowClassifierTests
{
    [Theory]
    [InlineData(Opcode.CMSG_SET_SELECTION, ProxyFlow.Selection)]
    [InlineData(Opcode.CMSG_LOOT_UNIT, ProxyFlow.Loot)]
    [InlineData(Opcode.CMSG_LOOT_ITEM, ProxyFlow.LootItem)]
    public void ClientRequests_AreClassifiedByLifecycle(Opcode opcode, ProxyFlow expected)
    {
        Assert.Equal(expected, ProxyFlowClassifier.ForClientRequest(opcode));
    }

    [Theory]
    [InlineData(Opcode.SMSG_LOOT_RESPONSE, ProxyFlow.LootResponse)]
    [InlineData(Opcode.SMSG_LOOT_LIST, ProxyFlow.LootList)]
    [InlineData(Opcode.SMSG_LOOT_MASTER_LIST, ProxyFlow.LootList)]
    [InlineData(Opcode.SMSG_UPDATE_OBJECT, ProxyFlow.ObjectUpdate)]
    public void LegacyResponses_AreClassifiedByLifecycle(Opcode opcode, ProxyFlow expected)
    {
        Assert.Equal(expected, ProxyFlowClassifier.ForLegacyResponse(opcode));
    }
}

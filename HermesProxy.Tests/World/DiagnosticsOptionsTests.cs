using HermesProxy.Configuration.Options;
using Xunit;

namespace HermesProxy.Tests.World;

public sealed class DiagnosticsOptionsTests
{
    [Fact]
    public void PacketCaptureIsDisabledByDefault()
    {
        Assert.False(new DiagnosticsOptions().PacketsLog);
    }
}

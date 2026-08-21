using System;
using System.IO;
using Xunit;

namespace HermesProxy.Tests.World;

public sealed class CharacterEnumRegressionTests
{
    [Fact]
    public void CharacterEnumerationRetainsConvertedCustomizations()
    {
        var repositoryRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var handler = Path.Combine(repositoryRoot, "HermesProxy", "World", "Client", "PacketHandlers", "CharacterHandler.cs");

        Assert.DoesNotContain("char1.Customizations.Clear()", File.ReadAllText(handler), StringComparison.Ordinal);
    }
}

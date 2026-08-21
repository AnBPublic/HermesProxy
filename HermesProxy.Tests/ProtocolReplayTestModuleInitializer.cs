using System.Runtime.CompilerServices;
using System.Reflection;
using HermesProxy;
using HermesProxy.Enums;

namespace HermesProxy.Tests.ProtocolReplay;

internal static class ProtocolReplayTestModuleInitializer
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        var bootstrap = typeof(ModernVersion).Assembly.GetType("HermesProxy.VersionBootstrap", throwOnError: true)!;
        bootstrap.GetField("ModernBuild", BindingFlags.Static | BindingFlags.NonPublic)!.SetValue(null, ClientVersionBuild.V3_4_3_54261);
        bootstrap.GetField("LegacyBuild", BindingFlags.Static | BindingFlags.NonPublic)!.SetValue(null, ClientVersionBuild.V3_3_5a_12340);
    }
}

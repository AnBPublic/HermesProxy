namespace HermesProxy.Configuration.Options;

public sealed class ProxyNetworkOptions
{
    public string ExternalAddress { get; set; } = "127.0.0.1";

    public int RestPort { get; set; } = 8081;

    public int BNetPort { get; set; } = 1119;

    public int RealmPort { get; set; } = 8084;

    public int InstancePort { get; set; } = 8086;

    /// <summary>
    /// Keeps small gameplay packets from being delayed by TCP Nagle negotiation.
    /// Set false for an A/B run that follows the modern client's request.
    /// </summary>
    public bool ForceTcpNoDelay { get; set; } = true;

    /// <summary>
    /// Optional path to a custom PKCS#12 (.pfx) certificate served on the BNet TLS endpoints
    /// (BNetPort/RestPort). When unset, the embedded TrinityCore-compatible certificate is used.
    /// </summary>
    public string? CertificatePfxPath { get; set; }

    /// <summary>Password for <see cref="CertificatePfxPath"/>; null for a passwordless pfx.</summary>
    public string? CertificatePfxPassword { get; set; }
}

namespace Somewhat.SignedJwt.AwsCertificateManager;

public sealed class AwsCertificateManagerOptions
{
    public string CertificateArn { get; set; } = string.Empty;

    public string RegionSystemName { get; set; } = string.Empty;

    public string ExportPassphrase { get; set; } = string.Empty;
}
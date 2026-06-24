namespace Somewhat.SignedJwt.AzureKeyVault;

public sealed class AzureKeyVaultCertificateOptions
{
    public string VaultUri { get; set; } = string.Empty;

    public string RetrievalMode { get; set; } = "Certificate";

    public string CertificateName { get; set; } = string.Empty;

    public string? CertificateVersion { get; set; }

    public string SecretName { get; set; } = string.Empty;

    public string? SecretVersion { get; set; }

    public string? Password { get; set; }
}
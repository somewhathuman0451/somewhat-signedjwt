namespace Somewhat.SignedJwt.GoogleSecretManager;

public sealed class GoogleSecretManagerCertificateOptions
{
    public string ProjectId { get; set; } = string.Empty;

    public string SecretId { get; set; } = string.Empty;

    public string SecretVersion { get; set; } = "latest";

    public string PayloadFormat { get; set; } = "Pkcs12Base64";

    public string? Password { get; set; }
}
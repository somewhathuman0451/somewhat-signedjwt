namespace Somewhat.SignedJwt;

public sealed class CertificateSourceOptions
{
    public string? Name { get; set; }

    public string Source { get; set; } = "PemFiles";

    public Dictionary<string, string> Parameters { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public string? CertificatePath { get; set; }

    public string? PrivateKeyPath { get; set; }

    public string? PfxPath { get; set; }

    public string? Password { get; set; }

    public string? InlineCertificatePem { get; set; }

    public string? InlinePrivateKeyPem { get; set; }
}
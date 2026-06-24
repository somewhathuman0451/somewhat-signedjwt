namespace Somewhat.SignedJwt;

public sealed class SigningCertificateRegistryOptions
{
    public string? DefaultCertificateName { get; set; }

    public List<CertificateSourceOptions> Certificates { get; set; } = [];
}
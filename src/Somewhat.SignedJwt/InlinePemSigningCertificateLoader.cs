using System.Security.Cryptography.X509Certificates;

namespace Somewhat.SignedJwt;

public sealed class InlinePemSigningCertificateLoader : ISigningCertificateLoader
{
    public const string LoaderSourceName = "InlinePem";

    public string SourceName => LoaderSourceName;

    public ValueTask<X509Certificate2> LoadAsync(CertificateSourceOptions options, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var certificatePem = CertificateLoaderUtilities.Require(options.InlineCertificatePem, nameof(options.InlineCertificatePem));
        var privateKeyPem = CertificateLoaderUtilities.Require(options.InlinePrivateKeyPem, nameof(options.InlinePrivateKeyPem));
        var certificate = X509Certificate2.CreateFromPem(certificatePem, privateKeyPem);
        return ValueTask.FromResult(CertificateLoaderUtilities.PromoteEphemeralCertificate(certificate));
    }
}
using System.Security.Cryptography.X509Certificates;

namespace Somewhat.SignedJwt;

public sealed class PemFilesSigningCertificateLoader : ISigningCertificateLoader
{
    public const string LoaderSourceName = "PemFiles";

    public string SourceName => LoaderSourceName;

    public ValueTask<X509Certificate2> LoadAsync(CertificateSourceOptions options, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var certificatePath = CertificateLoaderUtilities.Require(options.CertificatePath, nameof(options.CertificatePath));
        var privateKeyPath = CertificateLoaderUtilities.Require(options.PrivateKeyPath, nameof(options.PrivateKeyPath));
        var certificate = X509Certificate2.CreateFromPemFile(
            CertificateLoaderUtilities.ResolveAbsolutePath(certificatePath),
            CertificateLoaderUtilities.ResolveAbsolutePath(privateKeyPath));

        return ValueTask.FromResult(CertificateLoaderUtilities.PromoteEphemeralCertificate(certificate));
    }
}
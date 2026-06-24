using System.Security.Cryptography.X509Certificates;

namespace Somewhat.SignedJwt;

public sealed class PfxFileSigningCertificateLoader : ISigningCertificateLoader
{
    public const string LoaderSourceName = "PfxFile";

    public string SourceName => LoaderSourceName;

    public ValueTask<X509Certificate2> LoadAsync(CertificateSourceOptions options, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var pfxPath = CertificateLoaderUtilities.Require(options.PfxPath, nameof(options.PfxPath));
        var certificate = new X509Certificate2(
            CertificateLoaderUtilities.ResolveAbsolutePath(pfxPath),
            options.Password,
            X509KeyStorageFlags.EphemeralKeySet | X509KeyStorageFlags.Exportable);

        return ValueTask.FromResult(certificate);
    }
}
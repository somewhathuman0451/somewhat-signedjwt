using System.Security.Cryptography.X509Certificates;

namespace Somewhat.SignedJwt;

public interface ISigningCertificateLoader
{
    string SourceName { get; }

    ValueTask<X509Certificate2> LoadAsync(CertificateSourceOptions options, CancellationToken cancellationToken = default);
}
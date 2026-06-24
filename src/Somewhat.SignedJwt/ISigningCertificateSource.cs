using System.Security.Cryptography.X509Certificates;

namespace Somewhat.SignedJwt;

public interface ISigningCertificateSource
{
    ValueTask<X509Certificate2> GetCertificateAsync(string? certificateName = null, CancellationToken cancellationToken = default);
}
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Options;

namespace Somewhat.SignedJwt;

public sealed class ConfiguredSigningCertificateSource : ISigningCertificateSource
{
    private readonly IOptions<SigningCertificateRegistryOptions> _options;
    private readonly IReadOnlyDictionary<string, ISigningCertificateLoader> _loaders;

    public ConfiguredSigningCertificateSource(
        IOptions<SigningCertificateRegistryOptions> options,
        IEnumerable<ISigningCertificateLoader> loaders)
    {
        _options = options;
        _loaders = loaders.ToDictionary(loader => loader.SourceName, StringComparer.OrdinalIgnoreCase);
    }

    public async ValueTask<X509Certificate2> GetCertificateAsync(string? certificateName = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var options = ResolveCertificateOptions(certificateName, _options.Value);

        if (!_loaders.TryGetValue(options.Source, out var loader))
        {
            throw new InvalidOperationException($"Unsupported certificate source '{options.Source}'.");
        }

        var certificate = await loader.LoadAsync(options, cancellationToken);

        if (!certificate.HasPrivateKey)
        {
            certificate.Dispose();
            throw new InvalidOperationException("The signing certificate must include a private key.");
        }

        return certificate;
    }

    private static CertificateSourceOptions ResolveCertificateOptions(
        string? certificateName,
        SigningCertificateRegistryOptions registryOptions)
    {
        if (registryOptions.Certificates.Count == 0)
        {
            throw new InvalidOperationException("At least one signing certificate must be configured.");
        }

        if (!string.IsNullOrWhiteSpace(certificateName))
        {
            return registryOptions.Certificates.FirstOrDefault(options =>
                string.Equals(options.Name, certificateName, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException($"No signing certificate named '{certificateName}' is configured.");
        }

        if (!string.IsNullOrWhiteSpace(registryOptions.DefaultCertificateName))
        {
            return registryOptions.Certificates.FirstOrDefault(options =>
                string.Equals(options.Name, registryOptions.DefaultCertificateName, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException(
                    $"The default signing certificate '{registryOptions.DefaultCertificateName}' is not configured.");
        }

        if (registryOptions.Certificates.Count == 1)
        {
            return registryOptions.Certificates[0];
        }

        throw new InvalidOperationException(
            "Multiple signing certificates are configured. Specify a certificate name when generating the JWT.");
    }
}
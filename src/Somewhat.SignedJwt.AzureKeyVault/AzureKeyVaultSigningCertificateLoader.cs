using System.Security.Cryptography.X509Certificates;
using Azure;
using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Secrets;

namespace Somewhat.SignedJwt.AzureKeyVault;

public sealed class AzureKeyVaultSigningCertificateLoader : ISigningCertificateLoader
{
    public const string LoaderSourceName = "AzureKeyVault";
    public const string RetrievalModeParameter = "RetrievalMode";
    public const string CertificateNameParameter = "CertificateName";
    public const string CertificateVersionParameter = "CertificateVersion";
    public const string VaultUriParameter = "VaultUri";
    public const string SecretNameParameter = "SecretName";
    public const string SecretVersionParameter = "SecretVersion";

    private readonly IAzureKeyVaultSecretReader _secretReader;

    public AzureKeyVaultSigningCertificateLoader()
        : this(new AzureKeyVaultSecretReader())
    {
    }

    public AzureKeyVaultSigningCertificateLoader(IAzureKeyVaultSecretReader secretReader)
    {
        _secretReader = secretReader;
    }

    public string SourceName => LoaderSourceName;

    public async ValueTask<X509Certificate2> LoadAsync(CertificateSourceOptions options, CancellationToken cancellationToken = default)
    {
        var vaultUri = CertificateLoaderUtilities.RequireParameter(options, VaultUriParameter);
        var retrievalMode = options.Parameters.TryGetValue(RetrievalModeParameter, out var configuredMode)
            ? configuredMode
            : "Certificate";

        string secretName;
        string? secretVersion;

        if (string.Equals(retrievalMode, "Certificate", StringComparison.OrdinalIgnoreCase))
        {
            var certificateName = CertificateLoaderUtilities.RequireParameter(options, CertificateNameParameter);
            options.Parameters.TryGetValue(CertificateVersionParameter, out var certificateVersion);

            var reference = await _secretReader.GetCertificateSecretReferenceAsync(
                vaultUri,
                certificateName,
                certificateVersion,
                cancellationToken);

            secretName = reference.SecretName;
            secretVersion = reference.SecretVersion;
        }
        else if (string.Equals(retrievalMode, "Secret", StringComparison.OrdinalIgnoreCase))
        {
            secretName = CertificateLoaderUtilities.RequireParameter(options, SecretNameParameter);
            options.Parameters.TryGetValue(SecretVersionParameter, out secretVersion);
        }
        else
        {
            throw new InvalidOperationException(
                $"Unsupported Azure Key Vault retrieval mode '{retrievalMode}'. Use 'Certificate' or 'Secret'.");
        }

        var secret = await _secretReader.GetSecretAsync(vaultUri, secretName, secretVersion, cancellationToken);
        var secretValue = secret.Value;
        var contentType = secret.ContentType ?? string.Empty;

        if (contentType.Contains("pkcs12", StringComparison.OrdinalIgnoreCase)
            || contentType.Contains("pfx", StringComparison.OrdinalIgnoreCase)
            || !secretValue.Contains("-----BEGIN", StringComparison.Ordinal))
        {
            var pfxBytes = Convert.FromBase64String(secretValue);
            return new X509Certificate2(
                pfxBytes,
                options.Password,
                X509KeyStorageFlags.EphemeralKeySet | X509KeyStorageFlags.Exportable);
        }

        var certificate = X509Certificate2.CreateFromPem(secretValue, secretValue);
        return CertificateLoaderUtilities.PromoteEphemeralCertificate(certificate);
    }
}

public interface IAzureKeyVaultSecretReader
{
    Task<AzureKeyVaultCertificateSecretReference> GetCertificateSecretReferenceAsync(
        string vaultUri,
        string certificateName,
        string? certificateVersion,
        CancellationToken cancellationToken = default);

    Task<AzureKeyVaultSecretPayload> GetSecretAsync(
        string vaultUri,
        string secretName,
        string? secretVersion,
        CancellationToken cancellationToken = default);
}

public sealed class AzureKeyVaultSecretReader : IAzureKeyVaultSecretReader
{
    public async Task<AzureKeyVaultCertificateSecretReference> GetCertificateSecretReferenceAsync(
        string vaultUri,
        string certificateName,
        string? certificateVersion,
        CancellationToken cancellationToken = default)
    {
        var client = new CertificateClient(new Uri(vaultUri), new DefaultAzureCredential());
        Uri? secretIdentifier;

        if (string.IsNullOrWhiteSpace(certificateVersion))
        {
            var certificate = await client.GetCertificateAsync(certificateName, cancellationToken);
            secretIdentifier = certificate.Value.SecretId;
        }
        else
        {
            var certificate = await client.GetCertificateVersionAsync(certificateName, certificateVersion, cancellationToken);
            secretIdentifier = certificate.Value.SecretId;
        }

        if (secretIdentifier is null)
        {
            throw new InvalidOperationException(
                $"Certificate '{certificateName}' in vault '{vaultUri}' does not expose an associated secret ID.");
        }

        var (secretName, secretVersion) = ParseSecretIdentifier(secretIdentifier);
        return new AzureKeyVaultCertificateSecretReference(secretName, secretVersion);
    }

    public async Task<AzureKeyVaultSecretPayload> GetSecretAsync(
        string vaultUri,
        string secretName,
        string? secretVersion,
        CancellationToken cancellationToken = default)
    {
        var client = new SecretClient(new Uri(vaultUri), new DefaultAzureCredential());
        var secret = await client.GetSecretAsync(secretName, secretVersion, cancellationToken);
        return new AzureKeyVaultSecretPayload(secret.Value.Value, secret.Value.Properties.ContentType);
    }

    private static (string SecretName, string SecretVersion) ParseSecretIdentifier(Uri secretIdentifier)
    {
        var segments = secretIdentifier.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (segments.Length < 3 || !string.Equals(segments[0], "secrets", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Certificate secret ID '{secretIdentifier}' is not in the expected '/secrets/{{name}}/{{version}}' format.");
        }

        return (segments[1], segments[2]);
    }
}

public sealed record AzureKeyVaultSecretPayload(string Value, string? ContentType);

public sealed record AzureKeyVaultCertificateSecretReference(string SecretName, string SecretVersion);
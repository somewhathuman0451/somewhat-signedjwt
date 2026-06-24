using System.Security.Cryptography.X509Certificates;
using Google.Cloud.SecretManager.V1;

namespace Somewhat.SignedJwt.GoogleSecretManager;

public sealed class GoogleSecretManagerSigningCertificateLoader : ISigningCertificateLoader
{
    public const string LoaderSourceName = "GoogleSecretManager";
    public const string ProjectIdParameter = "ProjectId";
    public const string SecretIdParameter = "SecretId";
    public const string SecretVersionParameter = "SecretVersion";
    public const string PayloadFormatParameter = "PayloadFormat";

    private readonly IGoogleSecretPayloadReader _payloadReader;

    public GoogleSecretManagerSigningCertificateLoader()
        : this(new GoogleSecretPayloadReader())
    {
    }

    public GoogleSecretManagerSigningCertificateLoader(IGoogleSecretPayloadReader payloadReader)
    {
        _payloadReader = payloadReader;
    }

    public string SourceName => LoaderSourceName;

    public async ValueTask<X509Certificate2> LoadAsync(CertificateSourceOptions options, CancellationToken cancellationToken = default)
    {
        var projectId = CertificateLoaderUtilities.RequireParameter(options, ProjectIdParameter);
        var secretId = CertificateLoaderUtilities.RequireParameter(options, SecretIdParameter);
        var secretVersion = options.Parameters.TryGetValue(SecretVersionParameter, out var configuredVersion) && !string.IsNullOrWhiteSpace(configuredVersion)
            ? configuredVersion
            : "latest";
        var payloadFormat = options.Parameters.TryGetValue(PayloadFormatParameter, out var configuredFormat) && !string.IsNullOrWhiteSpace(configuredFormat)
            ? configuredFormat
            : "Pkcs12Base64";

        var payload = await _payloadReader.GetPayloadAsync(projectId, secretId, secretVersion, cancellationToken);

        if (string.Equals(payloadFormat, "PemBundle", StringComparison.OrdinalIgnoreCase))
        {
            var certificate = X509Certificate2.CreateFromPem(payload, payload);
            return CertificateLoaderUtilities.PromoteEphemeralCertificate(certificate);
        }

        var pfxBytes = Convert.FromBase64String(payload);
        return new X509Certificate2(
            pfxBytes,
            options.Password,
            X509KeyStorageFlags.EphemeralKeySet | X509KeyStorageFlags.Exportable);
    }
}

public interface IGoogleSecretPayloadReader
{
    Task<string> GetPayloadAsync(
        string projectId,
        string secretId,
        string secretVersion,
        CancellationToken cancellationToken = default);
}

public sealed class GoogleSecretPayloadReader : IGoogleSecretPayloadReader
{
    public async Task<string> GetPayloadAsync(
        string projectId,
        string secretId,
        string secretVersion,
        CancellationToken cancellationToken = default)
    {
        var client = await SecretManagerServiceClient.CreateAsync();
        var secretVersionName = new SecretVersionName(projectId, secretId, secretVersion);
        var secret = await client.AccessSecretVersionAsync(secretVersionName, cancellationToken);
        return secret.Payload.Data.ToStringUtf8();
    }
}
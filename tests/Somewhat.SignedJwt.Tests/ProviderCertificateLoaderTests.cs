using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Somewhat.SignedJwt.AwsCertificateManager;
using Somewhat.SignedJwt.AzureKeyVault;
using Somewhat.SignedJwt.GoogleSecretManager;

namespace Somewhat.SignedJwt.Tests;

public sealed class ProviderCertificateLoaderTests
{
    [Fact]
    public async Task AzureKeyVaultLoader_LoadsCertificateByCertificateName_WhenCertificateModeIsUsed()
    {
        using var certificate = CreateCertificate();
        var pfxBase64 = Convert.ToBase64String(certificate.Export(X509ContentType.Pkcs12));

        var loader = new AzureKeyVaultSigningCertificateLoader(
            new StubAzureSecretReader(
                certificateReference: new AzureKeyVaultCertificateSecretReference("jwt-cert-secret", "v1"),
                secretPayload: new AzureKeyVaultSecretPayload(pfxBase64, "application/x-pkcs12")));
        var options = new CertificateSourceOptions
        {
            Source = AzureKeyVaultSigningCertificateLoader.LoaderSourceName,
            Parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [AzureKeyVaultSigningCertificateLoader.RetrievalModeParameter] = "Certificate",
                [AzureKeyVaultSigningCertificateLoader.VaultUriParameter] = "https://demo.vault.azure.net/",
                [AzureKeyVaultSigningCertificateLoader.CertificateNameParameter] = "jwt-cert"
            }
        };

        using var loaded = await loader.LoadAsync(options);

        Assert.True(loaded.HasPrivateKey);
        Assert.NotNull(loaded.GetRSAPrivateKey());
    }

    [Fact]
    public async Task AzureKeyVaultLoader_LoadsCertificateBySecretName_WhenSecretModeIsUsed()
    {
        using var certificate = CreateCertificate();
        var pfxBase64 = Convert.ToBase64String(certificate.Export(X509ContentType.Pkcs12));

        var loader = new AzureKeyVaultSigningCertificateLoader(
            new StubAzureSecretReader(
                certificateReference: new AzureKeyVaultCertificateSecretReference("ignored", "ignored"),
                secretPayload: new AzureKeyVaultSecretPayload(pfxBase64, "application/x-pkcs12")));
        var options = new CertificateSourceOptions
        {
            Source = AzureKeyVaultSigningCertificateLoader.LoaderSourceName,
            Parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [AzureKeyVaultSigningCertificateLoader.RetrievalModeParameter] = "Secret",
                [AzureKeyVaultSigningCertificateLoader.VaultUriParameter] = "https://demo.vault.azure.net/",
                [AzureKeyVaultSigningCertificateLoader.SecretNameParameter] = "jwt-secret"
            }
        };

        using var loaded = await loader.LoadAsync(options);

        Assert.True(loaded.HasPrivateKey);
        Assert.NotNull(loaded.GetRSAPrivateKey());
    }

    [Fact]
    public async Task AwsCertificateManagerLoader_LoadsEncryptedPemCertificateFromMockedExporter()
    {
        const string passphrase = "integration-passphrase";

        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            "CN=Aws Provider Test",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        using var certificate = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddMinutes(-5),
            DateTimeOffset.UtcNow.AddHours(1));

        var certPem = PemEncoding.WriteString("CERTIFICATE", certificate.Export(X509ContentType.Cert));
        using var privateKey = certificate.GetRSAPrivateKey()!;
        var encryptedPrivateKeyPem = privateKey.ExportEncryptedPkcs8PrivateKeyPem(
            passphrase,
            new PbeParameters(
                PbeEncryptionAlgorithm.Aes256Cbc,
                HashAlgorithmName.SHA256,
                10_000));

        var loader = new AwsCertificateManagerSigningCertificateLoader(
            new StubAwsCertificateExporter(new AwsExportedCertificate(certPem, encryptedPrivateKeyPem)));
        var options = new CertificateSourceOptions
        {
            Source = AwsCertificateManagerSigningCertificateLoader.LoaderSourceName,
            Parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [AwsCertificateManagerSigningCertificateLoader.CertificateArnParameter] = "arn:aws:acm:us-east-1:111122223333:certificate/test",
                [AwsCertificateManagerSigningCertificateLoader.RegionSystemNameParameter] = "us-east-1",
                [AwsCertificateManagerSigningCertificateLoader.ExportPassphraseParameter] = passphrase
            }
        };

        using var loaded = await loader.LoadAsync(options);

        Assert.True(loaded.HasPrivateKey);
        Assert.NotNull(loaded.GetRSAPrivateKey());
    }

    [Fact]
    public async Task GoogleSecretManagerLoader_LoadsPkcs12Base64CertificateFromMockedReader()
    {
        using var certificate = CreateCertificate();
        var pfxBase64 = Convert.ToBase64String(certificate.Export(X509ContentType.Pkcs12));

        var loader = new GoogleSecretManagerSigningCertificateLoader(
            new StubGooglePayloadReader(pfxBase64));
        var options = new CertificateSourceOptions
        {
            Source = GoogleSecretManagerSigningCertificateLoader.LoaderSourceName,
            Parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [GoogleSecretManagerSigningCertificateLoader.ProjectIdParameter] = "demo-project",
                [GoogleSecretManagerSigningCertificateLoader.SecretIdParameter] = "jwt-certificate",
                [GoogleSecretManagerSigningCertificateLoader.SecretVersionParameter] = "latest",
                [GoogleSecretManagerSigningCertificateLoader.PayloadFormatParameter] = "Pkcs12Base64"
            }
        };

        using var loaded = await loader.LoadAsync(options);

        Assert.True(loaded.HasPrivateKey);
        Assert.NotNull(loaded.GetRSAPrivateKey());
    }

    private static X509Certificate2 CreateCertificate()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            "CN=Provider Loader Tests",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        using var certificate = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddMinutes(-5),
            DateTimeOffset.UtcNow.AddHours(1));

        return new X509Certificate2(
            certificate.Export(X509ContentType.Pkcs12),
            password: (string?)null,
            X509KeyStorageFlags.EphemeralKeySet | X509KeyStorageFlags.Exportable);
    }

    private sealed class StubAzureSecretReader : IAzureKeyVaultSecretReader
    {
        private readonly AzureKeyVaultCertificateSecretReference _certificateReference;
        private readonly AzureKeyVaultSecretPayload _payload;

        public StubAzureSecretReader(
            AzureKeyVaultCertificateSecretReference certificateReference,
            AzureKeyVaultSecretPayload secretPayload)
        {
            _certificateReference = certificateReference;
            _payload = secretPayload;
        }

        public Task<AzureKeyVaultCertificateSecretReference> GetCertificateSecretReferenceAsync(
            string vaultUri,
            string certificateName,
            string? certificateVersion,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_certificateReference);
        }

        public Task<AzureKeyVaultSecretPayload> GetSecretAsync(
            string vaultUri,
            string secretName,
            string? secretVersion,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_payload);
        }
    }

    private sealed class StubAwsCertificateExporter : IAwsCertificateExporter
    {
        private readonly AwsExportedCertificate _certificate;

        public StubAwsCertificateExporter(AwsExportedCertificate certificate)
        {
            _certificate = certificate;
        }

        public Task<AwsExportedCertificate> ExportAsync(
            string certificateArn,
            string regionSystemName,
            string exportPassphrase,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_certificate);
        }
    }

    private sealed class StubGooglePayloadReader : IGoogleSecretPayloadReader
    {
        private readonly string _payload;

        public StubGooglePayloadReader(string payload)
        {
            _payload = payload;
        }

        public Task<string> GetPayloadAsync(
            string projectId,
            string secretId,
            string secretVersion,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_payload);
        }
    }
}

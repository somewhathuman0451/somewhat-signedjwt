using System.Security.Cryptography.X509Certificates;
using Amazon;
using Amazon.CertificateManager;
using Amazon.CertificateManager.Model;

namespace Somewhat.SignedJwt.AwsCertificateManager;

public sealed class AwsCertificateManagerSigningCertificateLoader : ISigningCertificateLoader
{
    public const string LoaderSourceName = "AwsCertificateManager";
    public const string CertificateArnParameter = "CertificateArn";
    public const string RegionSystemNameParameter = "RegionSystemName";
    public const string ExportPassphraseParameter = "ExportPassphrase";

    private readonly IAwsCertificateExporter _certificateExporter;

    public AwsCertificateManagerSigningCertificateLoader()
        : this(new AwsCertificateExporter())
    {
    }

    public AwsCertificateManagerSigningCertificateLoader(IAwsCertificateExporter certificateExporter)
    {
        _certificateExporter = certificateExporter;
    }

    public string SourceName => LoaderSourceName;

    public async ValueTask<X509Certificate2> LoadAsync(CertificateSourceOptions options, CancellationToken cancellationToken = default)
    {
        var certificateArn = CertificateLoaderUtilities.RequireParameter(options, CertificateArnParameter);
        var regionSystemName = CertificateLoaderUtilities.RequireParameter(options, RegionSystemNameParameter);
        var exportPassphrase = CertificateLoaderUtilities.RequireParameter(options, ExportPassphraseParameter);

        var response = await _certificateExporter.ExportAsync(certificateArn, regionSystemName, exportPassphrase, cancellationToken);

        var certificate = X509Certificate2.CreateFromEncryptedPem(
            response.CertificatePem,
            response.PrivateKeyPem,
            exportPassphrase);

        return CertificateLoaderUtilities.PromoteEphemeralCertificate(certificate);
    }
}

public interface IAwsCertificateExporter
{
    Task<AwsExportedCertificate> ExportAsync(
        string certificateArn,
        string regionSystemName,
        string exportPassphrase,
        CancellationToken cancellationToken = default);
}

public sealed class AwsCertificateExporter : IAwsCertificateExporter
{
    public async Task<AwsExportedCertificate> ExportAsync(
        string certificateArn,
        string regionSystemName,
        string exportPassphrase,
        CancellationToken cancellationToken = default)
    {
        using var client = new AmazonCertificateManagerClient(RegionEndpoint.GetBySystemName(regionSystemName));
        var response = await client.ExportCertificateAsync(new ExportCertificateRequest
        {
            CertificateArn = certificateArn,
            Passphrase = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(exportPassphrase))
        }, cancellationToken);

        return new AwsExportedCertificate(response.Certificate, response.PrivateKey);
    }
}

public sealed record AwsExportedCertificate(string CertificatePem, string PrivateKeyPem);
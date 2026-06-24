using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Somewhat.SignedJwt.Sample.Shared;

public static class SampleCertificateMaterializer
{
    public static SampleCertificatePaths EnsureDevelopmentCertificate(string contentRootPath)
    {
        return EnsureDevelopmentCertificates(contentRootPath, "default").GetByName("default");
    }

    public static SampleCertificateSet EnsureDevelopmentCertificates(
        string contentRootPath,
        params string[] certificateNames)
    {
        var certificateDirectory = Path.GetFullPath(
            Path.Combine(contentRootPath, "..", "Shared", "Certificates"));

        Directory.CreateDirectory(certificateDirectory);

        var requestedNames = certificateNames.Length == 0 ? ["default"] : certificateNames;
        var certificates = new List<SampleCertificatePaths>(requestedNames.Length);

        foreach (var certificateName in requestedNames)
        {
            certificates.Add(EnsureDevelopmentCertificate(certificateDirectory, certificateName));
        }

        return new SampleCertificateSet(certificates);
    }

    private static SampleCertificatePaths EnsureDevelopmentCertificate(string certificateDirectory, string certificateName)
    {
        var normalizedName = certificateName.Trim().ToLowerInvariant();

        var certificatePath = Path.Combine(certificateDirectory, $"{normalizedName}-signing-cert.pem");
        var privateKeyPath = Path.Combine(certificateDirectory, $"{normalizedName}-signing-key.pem");

        if (File.Exists(certificatePath) && File.Exists(privateKeyPath))
        {
            if (TryLoadCertificatePair(certificatePath, privateKeyPath))
            {
                return new SampleCertificatePaths(certificateName, certificatePath, privateKeyPath);
            }

            File.Delete(certificatePath);
            File.Delete(privateKeyPath);
        }

        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            $"CN=Somewhat Signed JWT Sample {certificateName}",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        request.CertificateExtensions.Add(
            new X509BasicConstraintsExtension(false, false, 0, false));
        request.CertificateExtensions.Add(
            new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, false));
        request.CertificateExtensions.Add(
            new X509SubjectKeyIdentifierExtension(request.PublicKey, false));

        using var certificate = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddYears(2));

        File.WriteAllText(certificatePath, ExportCertificatePem(certificate), Encoding.ASCII);
        File.WriteAllText(privateKeyPath, ExportPrivateKeyPem(certificate), Encoding.ASCII);

        return new SampleCertificatePaths(certificateName, certificatePath, privateKeyPath);
    }

    private static bool TryLoadCertificatePair(string certificatePath, string privateKeyPath)
    {
        try
        {
            using var _ = X509Certificate2.CreateFromPemFile(certificatePath, privateKeyPath);
            return true;
        }
        catch (CryptographicException)
        {
            return false;
        }
    }

    private static string ExportCertificatePem(X509Certificate2 certificate)
    {
        return new string(PemEncoding.Write("CERTIFICATE", certificate.Export(X509ContentType.Cert)));
    }

    private static string ExportPrivateKeyPem(X509Certificate2 certificate)
    {
        using var privateKey = certificate.GetRSAPrivateKey()
            ?? throw new CryptographicException("The generated sample certificate does not contain an RSA private key.");

        return new string(PemEncoding.Write("PRIVATE KEY", privateKey.ExportPkcs8PrivateKey()));
    }
}

public sealed record SampleCertificatePaths(string Name, string CertificatePath, string PrivateKeyPath);

public sealed class SampleCertificateSet
{
    private readonly Dictionary<string, SampleCertificatePaths> _certificates;

    public SampleCertificateSet(IEnumerable<SampleCertificatePaths> certificates)
    {
        _certificates = certificates.ToDictionary(
            certificate => certificate.Name,
            certificate => certificate,
            StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyCollection<SampleCertificatePaths> Certificates => _certificates.Values;

    public SampleCertificatePaths GetByName(string name)
    {
        return _certificates.TryGetValue(name, out var certificate)
            ? certificate
            : throw new InvalidOperationException($"No sample certificate named '{name}' was generated.");
    }
}
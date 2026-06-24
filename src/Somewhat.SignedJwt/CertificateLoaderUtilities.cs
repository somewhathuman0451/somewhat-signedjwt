using System.Security.Cryptography.X509Certificates;

namespace Somewhat.SignedJwt;

public static class CertificateLoaderUtilities
{
    public static X509Certificate2 PromoteEphemeralCertificate(X509Certificate2 certificate)
    {
        return new X509Certificate2(
            certificate.Export(X509ContentType.Pkcs12),
            password: (string?)null,
            X509KeyStorageFlags.EphemeralKeySet | X509KeyStorageFlags.Exportable);
    }

    public static string Require(string? value, string parameterName)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        throw new InvalidOperationException($"Certificate source option '{parameterName}' is required.");
    }

    public static string ResolveAbsolutePath(string path)
    {
        return Path.IsPathRooted(path)
            ? path
            : Path.GetFullPath(path, AppContext.BaseDirectory);
    }

    public static string RequireParameter(CertificateSourceOptions options, string key)
    {
        return options.Parameters.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new InvalidOperationException($"Certificate source parameter '{key}' is required for source '{options.Source}'.");
    }
}
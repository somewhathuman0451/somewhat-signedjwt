namespace Somewhat.SignedJwt;

public sealed class SignedJwtAuthenticationOptions
{
    public string ApiKey { get; set; } = string.Empty;

    public string? CertificateName { get; set; }
}
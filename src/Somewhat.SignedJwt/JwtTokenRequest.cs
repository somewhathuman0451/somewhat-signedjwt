namespace Somewhat.SignedJwt;

public sealed class JwtTokenRequest
{
    public string? ApiKey { get; set; }

    public string? CertificateName { get; set; }
}
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Somewhat.SignedJwt;

public sealed class JwtTokenFactory : IJwtTokenFactory
{
    private readonly ISigningCertificateSource _certificateSource;
    private readonly IOptions<SignedJwtClientOptions> _options;
    private readonly JwtSecurityTokenHandler _tokenHandler = new();

    public JwtTokenFactory(
        ISigningCertificateSource certificateSource,
        IOptions<SignedJwtClientOptions> options)
    {
        _certificateSource = certificateSource;
        _options = options;
    }

    public async ValueTask<string> CreateTokenAsync(JwtTokenRequest? request = null, CancellationToken cancellationToken = default)
    {
        var options = _options.Value;
        var apiKey = string.IsNullOrWhiteSpace(request?.ApiKey)
            ? options.ApiKey
            : request.ApiKey;

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Signed JWT client option 'ApiKey' is required when no API key override is supplied.");
        }

        if (string.IsNullOrWhiteSpace(options.Audience))
        {
            throw new InvalidOperationException("Signed JWT client option 'Audience' is required.");
        }

        if (options.TokenLifetime <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("Signed JWT client option 'TokenLifetime' must be greater than zero.");
        }

        using var certificate = await _certificateSource.GetCertificateAsync(request?.CertificateName, cancellationToken);

        var issuedAt = DateTimeOffset.UtcNow;
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, string.IsNullOrWhiteSpace(options.Subject) ? apiKey : options.Subject),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new(options.ApiKeyClaimName, apiKey)
        };

        if (!string.IsNullOrWhiteSpace(options.Scope))
        {
            claims.Add(new Claim("scope", options.Scope));
        }

        var descriptor = new SecurityTokenDescriptor
        {
            Audience = options.Audience,
            Issuer = options.Issuer,
            Subject = new ClaimsIdentity(claims),
            IssuedAt = issuedAt.UtcDateTime,
            NotBefore = issuedAt.UtcDateTime,
            Expires = issuedAt.Add(options.TokenLifetime).UtcDateTime,
            SigningCredentials = new SigningCredentials(
                new X509SecurityKey(certificate),
                SecurityAlgorithms.RsaSha256)
        };

        var token = _tokenHandler.CreateToken(descriptor);
        return _tokenHandler.WriteToken(token);
    }
}
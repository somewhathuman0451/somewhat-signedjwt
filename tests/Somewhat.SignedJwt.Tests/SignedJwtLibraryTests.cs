using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Somewhat.SignedJwt.Tests;

public sealed class SignedJwtLibraryTests
{
    [Fact]
    public async Task JwtTokenFactory_EmbedsConfiguredClaimsAndSignsToken()
    {
        using var certificate = CreateCertificate();
        var certificateSource = new StubCertificateSource(certificate);
        var options = Options.Create(new SignedJwtClientOptions
        {
            ApiKey = "test-api-key",
            Issuer = "issuer-a",
            Audience = "audience-a",
            Subject = "subject-a",
            ApiKeyClaimName = "client_id",
            Scope = "quotes.read",
            TokenLifetime = TimeSpan.FromMinutes(3)
        });

        var factory = new JwtTokenFactory(certificateSource, options);

    var token = await factory.CreateTokenAsync();

        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Equal("issuer-a", parsed.Issuer);
        Assert.Equal("audience-a", Assert.Single(parsed.Audiences));
        Assert.Equal("subject-a", parsed.Subject);
        Assert.Equal("test-api-key", parsed.Claims.Single(claim => claim.Type == "client_id").Value);
        Assert.Equal("quotes.read", parsed.Claims.Single(claim => claim.Type == "scope").Value);
        Assert.Equal(SecurityAlgorithms.RsaSha256, parsed.Header.Alg);
        Assert.Equal("default", certificateSource.RequestedCertificateNames.Single());
    }

    [Fact]
    public async Task JwtTokenFactory_UsesApiKeyAndCertificateOverrideWhenProvided()
    {
        using var certificate = CreateCertificate();
        var certificateSource = new StubCertificateSource(certificate);
        var options = Options.Create(new SignedJwtClientOptions
        {
            ApiKey = "configured-api-key",
            Issuer = "issuer-a",
            Audience = "audience-a",
            Subject = string.Empty,
            ApiKeyClaimName = "client_id",
            TokenLifetime = TimeSpan.FromMinutes(3)
        });

        var factory = new JwtTokenFactory(certificateSource, options);

        var token = await factory.CreateTokenAsync(new JwtTokenRequest
        {
            ApiKey = "override-api-key",
            CertificateName = "secondary"
        });

        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Equal("override-api-key", parsed.Claims.Single(claim => claim.Type == "client_id").Value);
        Assert.Equal("override-api-key", parsed.Subject);
        Assert.Equal("secondary", certificateSource.RequestedCertificateNames.Single());
    }

    [Fact]
    public async Task SignedJwtAuthenticationHandler_AddsBearerTokenWhenRequestHasNoAuthorizationHeader()
    {
        var tokenFactory = new StubTokenFactory("signed-token");
        var innerHandler = new CaptureHandler();
        var handler = new SignedJwtAuthenticationHandler(tokenFactory)
        {
            InnerHandler = innerHandler
        };
        using var client = new HttpClient(handler);

        using var response = await client.GetAsync("http://localhost/test");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(innerHandler.CapturedRequest);
        Assert.Equal("Bearer", innerHandler.CapturedRequest!.Headers.Authorization?.Scheme);
        Assert.Equal("signed-token", innerHandler.CapturedRequest.Headers.Authorization?.Parameter);
    }

    [Fact]
    public async Task SignedJwtAuthenticationHandler_PassesInlineApiKeyAndCertificateNameToTokenFactory()
    {
        var tokenFactory = new CapturingTokenFactory("override-token");
        var innerHandler = new CaptureHandler();
        var handler = new SignedJwtAuthenticationHandler(
            tokenFactory,
            new SignedJwtAuthenticationOptions
            {
                ApiKey = "inline-api-key",
                CertificateName = "secondary"
            })
        {
            InnerHandler = innerHandler
        };
        using var client = new HttpClient(handler);

        using var response = await client.GetAsync("http://localhost/test");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(tokenFactory.CapturedRequest);
        Assert.Equal("inline-api-key", tokenFactory.CapturedRequest!.ApiKey);
        Assert.Equal("secondary", tokenFactory.CapturedRequest.CertificateName);
        Assert.Equal("Bearer", innerHandler.CapturedRequest!.Headers.Authorization?.Scheme);
        Assert.Equal("override-token", innerHandler.CapturedRequest.Headers.Authorization?.Parameter);
    }

    private static X509Certificate2 CreateCertificate()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            "CN=Somewhat SignedJwt Tests",
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

    private sealed class StubCertificateSource : ISigningCertificateSource
    {
        private readonly X509Certificate2 _certificate;

        public List<string> RequestedCertificateNames { get; } = [];

        public StubCertificateSource(X509Certificate2 certificate)
        {
            _certificate = certificate;
        }

        public ValueTask<X509Certificate2> GetCertificateAsync(string? certificateName = null, CancellationToken cancellationToken = default)
        {
            RequestedCertificateNames.Add(certificateName ?? "default");

            return ValueTask.FromResult(new X509Certificate2(
                _certificate.Export(X509ContentType.Pkcs12),
                password: (string?)null,
                X509KeyStorageFlags.EphemeralKeySet | X509KeyStorageFlags.Exportable));
        }
    }

    private sealed class StubTokenFactory : IJwtTokenFactory
    {
        private readonly string _token;

        public StubTokenFactory(string token)
        {
            _token = token;
        }

        public ValueTask<string> CreateTokenAsync(JwtTokenRequest? request = null, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(_token);
        }
    }

    private sealed class CapturingTokenFactory : IJwtTokenFactory
    {
        private readonly string _token;

        public CapturingTokenFactory(string token)
        {
            _token = token;
        }

        public JwtTokenRequest? CapturedRequest { get; private set; }

        public ValueTask<string> CreateTokenAsync(JwtTokenRequest? request = null, CancellationToken cancellationToken = default)
        {
            CapturedRequest = request;
            return ValueTask.FromResult(_token);
        }
    }

    private sealed class CaptureHandler : HttpMessageHandler
    {
        public HttpRequestMessage? CapturedRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CapturedRequest = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("ok")
            });
        }
    }
}
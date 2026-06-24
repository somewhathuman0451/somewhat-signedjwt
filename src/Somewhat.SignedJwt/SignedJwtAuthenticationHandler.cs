using System.Net.Http.Headers;

namespace Somewhat.SignedJwt;

public sealed class SignedJwtAuthenticationHandler : DelegatingHandler
{
    private readonly IJwtTokenFactory _tokenFactory;
    private readonly SignedJwtAuthenticationOptions _authenticationOptions;

    public SignedJwtAuthenticationHandler(
        IJwtTokenFactory tokenFactory,
        SignedJwtAuthenticationOptions? authenticationOptions = null)
    {
        _tokenFactory = tokenFactory;
        _authenticationOptions = authenticationOptions ?? new SignedJwtAuthenticationOptions();
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Headers.Authorization is null)
        {
            var tokenRequest = string.IsNullOrWhiteSpace(_authenticationOptions.ApiKey)
                && string.IsNullOrWhiteSpace(_authenticationOptions.CertificateName)
                ? null
                : new JwtTokenRequest
                {
                    ApiKey = _authenticationOptions.ApiKey,
                    CertificateName = _authenticationOptions.CertificateName
                };

            var token = await _tokenFactory.CreateTokenAsync(tokenRequest, cancellationToken);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
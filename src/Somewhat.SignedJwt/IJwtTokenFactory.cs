namespace Somewhat.SignedJwt;

public interface IJwtTokenFactory
{
    ValueTask<string> CreateTokenAsync(JwtTokenRequest? request = null, CancellationToken cancellationToken = default);
}
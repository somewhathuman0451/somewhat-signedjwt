namespace Somewhat.SignedJwt;

public sealed class SignedJwtClientOptions
{
    public string ApiKey { get; set; } = string.Empty;

    public string Issuer { get; set; } = "somewhat-signedjwt-client";

    public string Audience { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public string ApiKeyClaimName { get; set; } = "client_id";

    public string? Scope { get; set; }

    public TimeSpan TokenLifetime { get; set; } = TimeSpan.FromMinutes(5);
}
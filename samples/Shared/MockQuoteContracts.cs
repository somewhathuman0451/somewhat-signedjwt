namespace Somewhat.SignedJwt.Sample.Shared;

public sealed record MockQuoteRequest(string ProductCode, int Quantity, string CustomerReference);

public sealed record MockQuoteResponse(
    string ClientProfile,
    string CertificateName,
    string ProductCode,
    int Quantity,
    decimal UnitPrice,
    decimal TotalPrice,
    string AuthenticatedApiKey,
    string Subject,
    DateTimeOffset IssuedAtUtc,
    string Message);
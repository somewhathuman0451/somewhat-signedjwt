using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Somewhat.SignedJwt.Sample.Shared;

namespace Somewhat.SignedJwt.Tests;

public sealed class SampleIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public SampleIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PositiveProfile_IsAcceptedByMockService()
    {
        using var client = CreateSignedClient("demo-api-key-a", "alpha");

        using var response = await client.PostAsJsonAsync("/api/quotes", new MockQuoteRequest("WIDGET", 3, "integration-positive"));

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<MockQuoteResponse>();

        Assert.NotNull(payload);
        Assert.Equal("Standard Partner", payload.ClientProfile);
        Assert.Equal("alpha", payload.CertificateName);
        Assert.Equal("demo-api-key-a", payload.AuthenticatedApiKey);
    }

    [Fact]
    public async Task MismatchedCertificateProfile_IsRejectedByMockService()
    {
        using var client = CreateSignedClient("demo-api-key-a", "beta");

        using var response = await client.PostAsJsonAsync("/api/quotes", new MockQuoteRequest("WIDGET", 3, "integration-mismatch"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorPayload>();

        Assert.NotNull(payload);
        Assert.Equal("certificate_mismatch", payload.Error);
        Assert.Contains("Expected certificate 'alpha'", payload.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UnknownApiKeyProfile_IsRejectedByMockService()
    {
        using var client = CreateSignedClient("demo-api-key-unknown", "alpha");

        using var response = await client.PostAsJsonAsync("/api/quotes", new MockQuoteRequest("WIDGET", 3, "integration-unknown-key"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorPayload>();

        Assert.NotNull(payload);
        Assert.Equal("api_key_not_recognized", payload.Error);
    }

    private HttpClient CreateSignedClient(string apiKey, string certificateName)
    {
        var certificateSet = SampleCertificateMaterializer.EnsureDevelopmentCertificates(GetMockServiceContentRoot(), "alpha", "beta");
        var certificate = certificateSet.GetByName(certificateName);

        var services = new ServiceCollection();
        services.AddSignedJwtSupport(
            configureClient: options =>
            {
                options.Issuer = "somewhat-signedjwt-sample";
                options.Audience = "somewhat-signedjwt-mock-service";
                options.Subject = string.Empty;
                options.ApiKeyClaimName = "client_id";
                options.TokenLifetime = TimeSpan.FromMinutes(5);
            });
        services.AddSigningCertificate("alpha", options =>
        {
            options.Source = "PemFiles";
            options.CertificatePath = certificateSet.GetByName("alpha").CertificatePath;
            options.PrivateKeyPath = certificateSet.GetByName("alpha").PrivateKeyPath;
        });
        services.AddSigningCertificate("beta", options =>
        {
            options.Source = "PemFiles";
            options.CertificatePath = certificateSet.GetByName("beta").CertificatePath;
            options.PrivateKeyPath = certificateSet.GetByName("beta").PrivateKeyPath;
        });
        services.AddHttpClient("integration", client => client.BaseAddress = _factory.Server.BaseAddress)
            .ConfigurePrimaryHttpMessageHandler(() => _factory.Server.CreateHandler())
            .AddSignedJwtAuthentication(apiKey, certificateName);

        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IHttpClientFactory>().CreateClient("integration");
    }

    private static string GetMockServiceContentRoot()
    {
        return Path.GetFullPath("/workspaces/somewhat-signedjwt/samples/Somewhat.SignedJwt.Sample.MockService");
    }

    private sealed class ErrorPayload
    {
        public string Error { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;
    }
}
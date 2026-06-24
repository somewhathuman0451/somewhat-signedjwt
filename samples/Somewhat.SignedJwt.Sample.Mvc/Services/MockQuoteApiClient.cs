using System.Net.Http.Json;
using System.Net;
using Somewhat.SignedJwt.Sample.Shared;

namespace Somewhat.SignedJwt.Sample.Mvc.Services;

public interface IMockQuoteApiClient
{
    Task<MockQuoteResponse> GetQuoteAsync(string clientProfileName, MockQuoteRequest request, CancellationToken cancellationToken = default);
}

public sealed class MockQuoteApiClient : IMockQuoteApiClient
{
    private readonly IHttpClientFactory _httpClientFactory;

    public MockQuoteApiClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<MockQuoteResponse> GetQuoteAsync(string clientProfileName, MockQuoteRequest request, CancellationToken cancellationToken = default)
    {
        using var client = _httpClientFactory.CreateClient(clientProfileName);
        using var response = await client.PostAsJsonAsync("api/quotes", request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<MockServiceErrorResponse>(cancellationToken: cancellationToken);

            if (error is not null)
            {
                throw new MockServiceRejectedException(
                    response.StatusCode,
                    error.Error,
                    error.Message);
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new MockServiceRejectedException(
                response.StatusCode,
                "mock_service_error",
                $"Mock service rejected the request with status {(int)response.StatusCode}: {responseBody}");
        }

        return await response.Content.ReadFromJsonAsync<MockQuoteResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Mock service returned an empty response body.");
    }
}

public sealed class MockServiceRejectedException : Exception
{
    public MockServiceRejectedException(HttpStatusCode statusCode, string errorCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
    }

    public HttpStatusCode StatusCode { get; }

    public string ErrorCode { get; }
}

public sealed class MockServiceErrorResponse
{
    public string Error { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}

public sealed class MockQuoteApiOptions
{
    public const string SectionName = "MockQuoteApi";

    public string BaseAddress { get; set; } = "http://localhost:5130/";
}

public sealed class DemoClientProfilesOptions
{
    public const string SectionName = "DemoClientProfiles";

    public string DefaultProfileName { get; set; } = string.Empty;

    public List<DemoClientProfileOptions> Profiles { get; set; } = [];
}

public sealed class DemoClientProfileOptions
{
    public string Name { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    public string CertificateName { get; set; } = string.Empty;

    public bool ExpectedSuccess { get; set; } = true;

    public string Description { get; set; } = string.Empty;
}
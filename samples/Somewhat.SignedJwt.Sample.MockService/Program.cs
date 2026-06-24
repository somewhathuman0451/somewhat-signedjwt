using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Somewhat.SignedJwt.Sample.Shared;

var builder = WebApplication.CreateBuilder(args);

var mockJwtOptions = builder.Configuration.GetSection(MockJwtOptions.SectionName).Get<MockJwtOptions>() ?? new MockJwtOptions();
var certificateNames = mockJwtOptions.Clients.Select(client => client.CertificateName).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
var certificates = SampleCertificateMaterializer.EnsureDevelopmentCertificates(builder.Environment.ContentRootPath, certificateNames);
var certificateThumbprints = certificates.Certificates.ToDictionary(
	certificate => certificate.Name,
	certificate => X509Certificate2.CreateFromPemFile(certificate.CertificatePath, certificate.PrivateKeyPath).Thumbprint,
	StringComparer.OrdinalIgnoreCase);
var certificateNamesByThumbprint = certificateThumbprints.ToDictionary(
	entry => entry.Value,
	entry => entry.Key,
	StringComparer.OrdinalIgnoreCase);
var signingKeys = certificates.Certificates.Select(certificate =>
{
	var signingCertificate = X509Certificate2.CreateFromPemFile(certificate.CertificatePath, certificate.PrivateKeyPath);
	return new X509SecurityKey(signingCertificate);
}).ToArray();

builder.Services.Configure<MockJwtOptions>(builder.Configuration.GetSection(MockJwtOptions.SectionName));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
	.AddJwtBearer(options =>
	{
		options.TokenValidationParameters = new TokenValidationParameters
		{
			ValidateIssuer = true,
			ValidIssuer = mockJwtOptions.Issuer,
			ValidateAudience = true,
			ValidAudience = mockJwtOptions.Audience,
			ValidateIssuerSigningKey = true,
			IssuerSigningKeys = signingKeys,
			ValidateLifetime = true,
			ClockSkew = TimeSpan.FromSeconds(5),
			NameClaimType = JwtRegisteredClaimNames.Sub
		};
	});
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", (IOptions<MockJwtOptions> options) => Results.Ok(new
{
	service = "mock-quote-service",
	issuer = options.Value.Issuer,
	audience = options.Value.Audience,
	clients = options.Value.Clients.Select(client => new { client.Name, client.ApiKey, client.CertificateName }),
	certificates = certificates.Certificates.Select(certificate => new { certificate.Name, certificate.CertificatePath })
}));

app.MapPost("/api/quotes", (
	HttpRequest httpRequest,
	MockQuoteRequest request,
	ClaimsPrincipal user,
	IOptions<MockJwtOptions> options) =>
{
	var apiKey = user.FindFirstValue(options.Value.ApiKeyClaimName);
	var client = options.Value.Clients.FirstOrDefault(client =>
		string.Equals(client.ApiKey, apiKey, StringComparison.Ordinal));

	if (client is null)
	{
		return Results.Json(new
		{
			error = "api_key_not_recognized",
			message = "The mock service does not recognize the API key from the JWT."
		}, statusCode: StatusCodes.Status403Forbidden);
	}

	var rawToken = httpRequest.Headers.Authorization.ToString().Replace("Bearer ", string.Empty, StringComparison.OrdinalIgnoreCase);
	var token = new JwtSecurityTokenHandler().ReadJwtToken(rawToken);
	var expectedThumbprint = certificateThumbprints[client.CertificateName];
	var presentedCertificateName = certificateNamesByThumbprint.TryGetValue(token.Header.Kid ?? string.Empty, out var resolvedCertificateName)
		? resolvedCertificateName
		: token.Header.Kid ?? "unknown";

	if (!string.Equals(token.Header.Kid, expectedThumbprint, StringComparison.OrdinalIgnoreCase))
	{
		return Results.Json(new
		{
			error = "certificate_mismatch",
			message = $"API key '{apiKey}' is not allowed to sign with certificate '{presentedCertificateName}'. Expected certificate '{client.CertificateName}'."
		}, statusCode: StatusCodes.Status403Forbidden);
	}

	var issuedAt = long.TryParse(user.FindFirstValue(JwtRegisteredClaimNames.Iat), out var issuedAtSeconds)
		? DateTimeOffset.FromUnixTimeSeconds(issuedAtSeconds)
		: DateTimeOffset.UtcNow;

	var unitPrice = request.ProductCode.Trim().ToUpperInvariant() switch
	{
		"WIDGET" => 14.50m,
		"ROUTER" => 89.00m,
		"SENSOR" => 32.25m,
		_ => 9.99m
	};

	return Results.Ok(new MockQuoteResponse(
		client.Name,
		client.CertificateName,
		request.ProductCode,
		request.Quantity,
		unitPrice,
		request.Quantity * unitPrice,
		apiKey ?? string.Empty,
		user.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? string.Empty,
		issuedAt,
		$"Signed request accepted for {request.CustomerReference}."));
})
	.RequireAuthorization();

app.Run();

public partial class Program;

public sealed class MockJwtOptions
{
	public const string SectionName = "MockJwt";

	public string Issuer { get; set; } = "somewhat-signedjwt-sample";

	public string Audience { get; set; } = "somewhat-signedjwt-mock-service";

	public string ApiKeyClaimName { get; set; } = "client_id";

	public List<MockJwtClientOptions> Clients { get; set; } = [];
}

public sealed class MockJwtClientOptions
{
	public string Name { get; set; } = string.Empty;

	public string ApiKey { get; set; } = string.Empty;

	public string CertificateName { get; set; } = string.Empty;
}

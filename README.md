# Somewhat.SignedJwt

`Somewhat.SignedJwt` is a .NET 8 class library for calling APIs that require a certificate-signed JWT derived from an API key. It includes:

- A configurable certificate source abstraction with multi-certificate support.
- JWT generation with API key and scope claims.
- `HttpClient` integration through a delegating handler.
- A runnable sample UI and mock service that validate the end-to-end flow.

## Solution Layout

- `src/Somewhat.SignedJwt`: reusable library.
- `src/Somewhat.SignedJwt.AzureKeyVault`: optional Azure Key Vault certificate source package.
- `src/Somewhat.SignedJwt.AwsCertificateManager`: optional AWS Certificate Manager certificate source package.
- `src/Somewhat.SignedJwt.GoogleSecretManager`: optional Google Secret Manager certificate source package.
- `samples/Somewhat.SignedJwt.Sample.Mvc`: MVC UI that calls the mock service using the library.
- `samples/Somewhat.SignedJwt.Sample.MockService`: API that validates the signed JWT.
- `tests/Somewhat.SignedJwt.Tests`: focused unit tests for token generation and `HttpClient` integration.

The sample and mock now demonstrate multiple client profiles. Each profile uses a different API key and a different named signing certificate so you can see certificate selection and API key override working together.

## Certificate Sources

The library supports these certificate source modes through `CertificateSourceOptions`:

- `PemFiles`: loads a certificate PEM file and a private key PEM file.
- `PfxFile`: loads a PKCS#12 file with optional password.
- `InlinePem`: loads PEM content directly from configuration.
- `AzureKeyVault`: provided by `Somewhat.SignedJwt.AzureKeyVault`.
- `AwsCertificateManager`: provided by `Somewhat.SignedJwt.AwsCertificateManager`.
- `GoogleSecretManager`: provided by `Somewhat.SignedJwt.GoogleSecretManager`.

You can register more than one certificate and choose which one a client uses when calling `AddSignedJwtAuthentication()`.

Cloud providers are intentionally split into separate class libraries so each consuming solution only references what it needs.

Provider package docs:

- Azure Key Vault: [src/Somewhat.SignedJwt.AzureKeyVault/README.md](src/Somewhat.SignedJwt.AzureKeyVault/README.md)
- AWS Certificate Manager: [src/Somewhat.SignedJwt.AwsCertificateManager/README.md](src/Somewhat.SignedJwt.AwsCertificateManager/README.md)
- Google Secret Manager: [src/Somewhat.SignedJwt.GoogleSecretManager/README.md](src/Somewhat.SignedJwt.GoogleSecretManager/README.md)

## Basic Usage

Register the library and bind the options from configuration:

```csharp
builder.Services.AddSignedJwtSupport(
    builder.Configuration.GetSection("SignedJwtClient"),
    builder.Configuration.GetSection("CertificateSource"));

builder.Services.AddSigningCertificate("secondary", options =>
{
  options.Source = "PfxFile";
  options.PfxPath = "/path/to/secondary-signing-certificate.pfx";
  options.Password = "certificate-password";
});

builder.Services.AddHttpClient<MyApiClient>(client =>
    {
        client.BaseAddress = new Uri("https://partner-api.example/");
    })
  .AddSignedJwtAuthentication("runtime-api-key", "secondary");
```

Example configuration:

```json
{
  "SignedJwtClient": {
    "Issuer": "somewhat-signedjwt-sample",
    "Audience": "somewhat-signedjwt-mock-service",
    "Subject": "demo-client",
    "ApiKeyClaimName": "client_id",
    "Scope": "quotes.read",
    "TokenLifetime": "00:05:00"
  },
  "CertificateSource": {
    "DefaultCertificateName": "primary",
    "Certificates": [
      {
        "Name": "primary",
        "Source": "PemFiles",
        "CertificatePath": "/path/to/signing-cert.pem",
        "PrivateKeyPath": "/path/to/signing-key.pem"
      },
      {
        "Name": "secondary",
        "Source": "PfxFile",
        "PfxPath": "/path/to/backup-signing-certificate.pfx",
        "Password": "secret"
      }
    ]
  }
}
```

If you prefer, you can still keep a default `ApiKey` in `SignedJwtClient` and call `.AddSignedJwtAuthentication()` without parameters. Passing the API key in code overrides the configured value for that client only.

## Optional Cloud Provider Packages

Reference only the provider package you need.

### Azure Key Vault

```csharp
using Somewhat.SignedJwt.AzureKeyVault;

builder.Services.AddSignedJwtSupport(
  builder.Configuration.GetSection("SignedJwtClient"),
  builder.Configuration.GetSection("CertificateSource"));

builder.Services.AddAzureKeyVaultSigningCertificate("azure-signing", options =>
{
  options.VaultUri = "https://my-vault.vault.azure.net/";
  options.RetrievalMode = "Certificate"; // default
  options.CertificateName = "jwt-signing-certificate";
  options.CertificateVersion = "";
  options.Password = "";
});
```

To read a secret directly instead of resolving a Key Vault certificate first, set `RetrievalMode = "Secret"` and provide `SecretName` (and optional `SecretVersion`).

### AWS Certificate Manager

```csharp
using Somewhat.SignedJwt.AwsCertificateManager;

builder.Services.AddSignedJwtSupport(
  builder.Configuration.GetSection("SignedJwtClient"),
  builder.Configuration.GetSection("CertificateSource"));

builder.Services.AddAwsCertificateManagerSigningCertificate("aws-signing", options =>
{
  options.CertificateArn = "arn:aws:acm:us-east-1:111122223333:certificate/aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee";
  options.RegionSystemName = "us-east-1";
  options.ExportPassphrase = "export-passphrase";
});
```

### Google Secret Manager

```csharp
using Somewhat.SignedJwt.GoogleSecretManager;

builder.Services.AddSignedJwtSupport(
  builder.Configuration.GetSection("SignedJwtClient"),
  builder.Configuration.GetSection("CertificateSource"));

builder.Services.AddGoogleSecretManagerSigningCertificate("gcp-signing", options =>
{
  options.ProjectId = "my-project";
  options.SecretId = "jwt-signing-certificate";
  options.SecretVersion = "latest";
  options.PayloadFormat = "Pkcs12Base64";
  options.Password = "";
});
```

## Running The Sample

From the repository root:

```bash
dotnet restore
dotnet build
```

Start the mock service in one terminal:

```bash
cd samples/Somewhat.SignedJwt.Sample.MockService
dotnet run
```

Start the MVC demo in a second terminal:

```bash
cd samples/Somewhat.SignedJwt.Sample.Mvc
dotnet run
```

Then open `http://localhost:5009`, switch between the available client profiles, and submit the form. Each profile uses a different API key and certificate pair. The mock service response shows:

- the selected client profile
- the validated certificate name
- the authenticated API key
- the JWT subject

The sample also includes two negative demo profiles:

- `Negative Demo: Mismatched Certificate`: uses a valid API key with the wrong certificate so the mock returns a readable `403` certificate mismatch.
- `Negative Demo: Unknown API Key`: uses a certificate the mock recognizes with an API key the mock does not know, so the mock returns a readable `403` API key rejection.

Both samples materialize shared self-signed development certificates at `samples/Shared/Certificates/` on first run so the UI and the mock API stay aligned automatically.

## Validation

Run the unit tests:

```bash
dotnet test
```

The test suite includes integration coverage for:

- a successful request using a valid key and certificate pair
- a rejected request caused by a certificate mismatch
- a rejected request caused by an unknown API key
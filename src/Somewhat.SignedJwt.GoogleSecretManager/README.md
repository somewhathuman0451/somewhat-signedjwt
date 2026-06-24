# Somewhat.SignedJwt.GoogleSecretManager

Google Secret Manager certificate source support for Somewhat.SignedJwt.

## Install

Add a reference to this package and to the core package:

- Somewhat.SignedJwt
- Somewhat.SignedJwt.GoogleSecretManager

## Register

```csharp
using Somewhat.SignedJwt;
using Somewhat.SignedJwt.GoogleSecretManager;

builder.Services.AddSignedJwtSupport(
    builder.Configuration.GetSection("SignedJwtClient"),
    builder.Configuration.GetSection("CertificateSource"));

builder.Services.AddGoogleSecretManagerSigningCertificate("gcp-signing", options =>
{
    options.ProjectId = "my-project";
    options.SecretId = "jwt-signing-cert";
    options.SecretVersion = "latest";
    options.PayloadFormat = "Pkcs12Base64"; // or PemBundle
    options.Password = ""; // optional for PFX payloads
});
```

## Configuration Mapping

The extension maps options to `CertificateSourceOptions` like this:

- Source: `GoogleSecretManager`
- Parameters[ProjectId]
- Parameters[SecretId]
- Parameters[SecretVersion]
- Parameters[PayloadFormat]
- Password

## Secret Payload Formats

The loader supports:

- `Pkcs12Base64`: base64 PKCS#12/PFX payload
- `PemBundle`: PEM certificate/key bundle payload

## Required GCP Permissions

The runtime principal needs at least:

- `secretmanager.versions.access` on the target secret version

A common predefined role is:

- Secret Manager Secret Accessor (`roles/secretmanager.secretAccessor`)

## Notes

- Prefer Workload Identity or service account auth in production.
- Keep secret payloads and passwords out of checked-in config.

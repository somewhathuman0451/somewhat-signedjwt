# Somewhat.SignedJwt.AzureKeyVault

Azure Key Vault certificate source support for Somewhat.SignedJwt.

## Install

Add a reference to this package and to the core package:

- Somewhat.SignedJwt
- Somewhat.SignedJwt.AzureKeyVault

## Register

```csharp
using Somewhat.SignedJwt;
using Somewhat.SignedJwt.AzureKeyVault;

builder.Services.AddSignedJwtSupport(
    builder.Configuration.GetSection("SignedJwtClient"),
    builder.Configuration.GetSection("CertificateSource"));

builder.Services.AddAzureKeyVaultSigningCertificate("azure-signing", options =>
{
    options.VaultUri = "https://my-vault.vault.azure.net/";
    options.RetrievalMode = "Certificate"; // default
    options.CertificateName = "jwt-signing-cert";
    options.CertificateVersion = ""; // optional
    options.Password = ""; // optional for PFX payloads
});
```

## Configuration Mapping

The extension maps options to `CertificateSourceOptions` like this:

- Source: `AzureKeyVault`
- Parameters[RetrievalMode]
- Parameters[CertificateName]
- Parameters[CertificateVersion]
- Parameters[VaultUri]
- Parameters[SecretName]
- Parameters[SecretVersion]
- Password

## Retrieval Modes

The loader supports both Key Vault certificate and secret flows:

- `Certificate` mode (default): resolves a Key Vault certificate and then loads its backing secret value.
- `Secret` mode: reads a Key Vault secret directly.

## Secret Payload Formats

The loader supports:

- Base64 PKCS#12/PFX secret values
- PEM bundle secret values (certificate + private key)

## Required Azure Permissions

Use a managed identity or service principal that can read certificates and secrets:

- Key Vault Certificates Officer/User role (RBAC) or equivalent certificate read policy
- Key Vault Secrets User role (RBAC), or equivalent secret read policy
- Access to `certificates/get` when using `Certificate` mode
- Access to `secrets/get` on the target secret

## Notes

- Keep secret values out of appsettings where possible.
- Prefer managed identity in production over client secrets.

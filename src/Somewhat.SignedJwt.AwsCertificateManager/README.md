# Somewhat.SignedJwt.AwsCertificateManager

AWS Certificate Manager source support for Somewhat.SignedJwt.

## Install

Add a reference to this package and to the core package:

- Somewhat.SignedJwt
- Somewhat.SignedJwt.AwsCertificateManager

## Register

```csharp
using Somewhat.SignedJwt;
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

## Configuration Mapping

The extension maps options to `CertificateSourceOptions` like this:

- Source: `AwsCertificateManager`
- Parameters[CertificateArn]
- Parameters[RegionSystemName]
- Parameters[ExportPassphrase]

## Required AWS Permissions

The runtime principal needs at least:

- `acm:ExportCertificate` on the target certificate ARN

If your runtime also discovers certificate metadata elsewhere, you may additionally need:

- `acm:DescribeCertificate`

## Notes

- ACM export requires exportable private key certificates.
- Keep export passphrases in a secret store, not in source control.
- Align region with the certificate ARN region.

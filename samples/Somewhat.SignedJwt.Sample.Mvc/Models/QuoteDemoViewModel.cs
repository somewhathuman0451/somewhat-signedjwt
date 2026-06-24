using System.ComponentModel.DataAnnotations;
using Somewhat.SignedJwt.Sample.Shared;

namespace Somewhat.SignedJwt.Sample.Mvc.Models;

public sealed class QuoteDemoViewModel
{
    [Required]
    [Display(Name = "Client Profile")]
    public string ClientProfileName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Product Code")]
    public string ProductCode { get; set; } = string.Empty;

    [Range(1, 500)]
    public int Quantity { get; set; } = 1;

    [Required]
    [Display(Name = "Customer Reference")]
    public string CustomerReference { get; set; } = string.Empty;

    public string MockServiceBaseAddress { get; set; } = string.Empty;

    public string JwtIssuer { get; set; } = string.Empty;

    public string JwtAudience { get; set; } = string.Empty;

    public string ApiKeyPreview { get; set; } = string.Empty;

    public string CertificateName { get; set; } = string.Empty;

    public string CertificatePath { get; set; } = string.Empty;

    public bool ExpectedSuccess { get; set; } = true;

    public string ProfileDescription { get; set; } = string.Empty;

    public IReadOnlyList<ClientProfileViewModel> AvailableProfiles { get; set; } = [];

    public MockQuoteResponse? Response { get; set; }

    public string? ErrorCode { get; set; }

    public string? ErrorMessage { get; set; }
}

public sealed class ClientProfileViewModel
{
    public string Name { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    public string CertificateName { get; set; } = string.Empty;

    public bool ExpectedSuccess { get; set; } = true;

    public string Description { get; set; } = string.Empty;
}
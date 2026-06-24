using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Somewhat.SignedJwt;
using Somewhat.SignedJwt.Sample.Mvc.Models;
using Somewhat.SignedJwt.Sample.Mvc.Services;
using Somewhat.SignedJwt.Sample.Shared;

namespace Somewhat.SignedJwt.Sample.Mvc.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IMockQuoteApiClient _mockQuoteApiClient;
    private readonly MockQuoteApiOptions _mockQuoteApiOptions;
    private readonly DemoClientProfilesOptions _clientProfilesOptions;
    private readonly SignedJwtClientOptions _signedJwtOptions;
    private readonly SampleCertificateSet _certificates;

    public HomeController(
        ILogger<HomeController> logger,
        IMockQuoteApiClient mockQuoteApiClient,
        IOptions<MockQuoteApiOptions> mockQuoteApiOptions,
        IOptions<DemoClientProfilesOptions> clientProfilesOptions,
        IOptions<SignedJwtClientOptions> signedJwtOptions,
        SampleCertificateSet certificates)
    {
        _logger = logger;
        _mockQuoteApiClient = mockQuoteApiClient;
        _mockQuoteApiOptions = mockQuoteApiOptions.Value;
        _clientProfilesOptions = clientProfilesOptions.Value;
        _signedJwtOptions = signedJwtOptions.Value;
        _certificates = certificates;
    }

    public IActionResult Index()
    {
        return View(CreateDefaultModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(QuoteDemoViewModel model, CancellationToken cancellationToken)
    {
        HydrateReadOnlyFields(model);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            model.Response = await _mockQuoteApiClient.GetQuoteAsync(
                model.ClientProfileName,
                new MockQuoteRequest(model.ProductCode, model.Quantity, model.CustomerReference),
                cancellationToken);
        }
        catch (MockServiceRejectedException exception)
        {
            _logger.LogWarning(exception, "Quote request was rejected by the mock service.");
            model.ErrorCode = exception.ErrorCode;
            model.ErrorMessage = exception.Message;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Quote request failed.");
            model.ErrorMessage = exception.Message;
        }

        return View(model);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    private QuoteDemoViewModel CreateDefaultModel()
    {
        var model = new QuoteDemoViewModel
        {
            ClientProfileName = GetDefaultProfile().Name,
            ProductCode = "WIDGET",
            Quantity = 10,
            CustomerReference = "CONTOSO-2026"
        };

        HydrateReadOnlyFields(model);
        return model;
    }

    private void HydrateReadOnlyFields(QuoteDemoViewModel model)
    {
        var selectedProfile = GetSelectedProfile(model.ClientProfileName);
        var certificate = _certificates.GetByName(selectedProfile.CertificateName);

        model.MockServiceBaseAddress = _mockQuoteApiOptions.BaseAddress;
        model.JwtIssuer = _signedJwtOptions.Issuer;
        model.JwtAudience = _signedJwtOptions.Audience;
        model.ApiKeyPreview = selectedProfile.ApiKey;
        model.CertificateName = selectedProfile.CertificateName;
        model.CertificatePath = certificate.CertificatePath;
        model.ExpectedSuccess = selectedProfile.ExpectedSuccess;
        model.ProfileDescription = selectedProfile.Description;
        model.ClientProfileName = selectedProfile.Name;
        model.AvailableProfiles = _clientProfilesOptions.Profiles.Select(profile => new ClientProfileViewModel
        {
            Name = profile.Name,
            DisplayName = profile.DisplayName,
            ApiKey = profile.ApiKey,
            CertificateName = profile.CertificateName,
            ExpectedSuccess = profile.ExpectedSuccess,
            Description = profile.Description
        }).ToList();
    }

    private DemoClientProfileOptions GetDefaultProfile()
    {
        if (!string.IsNullOrWhiteSpace(_clientProfilesOptions.DefaultProfileName))
        {
            return GetSelectedProfile(_clientProfilesOptions.DefaultProfileName);
        }

        return _clientProfilesOptions.Profiles.First();
    }

    private DemoClientProfileOptions GetSelectedProfile(string profileName)
    {
        return _clientProfilesOptions.Profiles.FirstOrDefault(profile =>
                   string.Equals(profile.Name, profileName, StringComparison.OrdinalIgnoreCase))
               ?? GetDefaultProfile();
    }
}

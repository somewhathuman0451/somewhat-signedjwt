using Microsoft.Extensions.Options;
using Somewhat.SignedJwt;
using Somewhat.SignedJwt.Sample.Mvc.Services;
using Somewhat.SignedJwt.Sample.Shared;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<DemoClientProfilesOptions>(builder.Configuration.GetSection(DemoClientProfilesOptions.SectionName));
var clientProfiles = builder.Configuration.GetSection(DemoClientProfilesOptions.SectionName).Get<DemoClientProfilesOptions>()
    ?? new DemoClientProfilesOptions();
var certificateNames = clientProfiles.Profiles.Select(profile => profile.CertificateName).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
var certificates = SampleCertificateMaterializer.EnsureDevelopmentCertificates(builder.Environment.ContentRootPath, certificateNames);

builder.Services.Configure<MockQuoteApiOptions>(builder.Configuration.GetSection(MockQuoteApiOptions.SectionName));
builder.Services.AddSignedJwtSupport(
    builder.Configuration.GetSection("SignedJwtClient"),
    builder.Configuration.GetSection("CertificateSource"));
builder.Services.PostConfigure<SigningCertificateRegistryOptions>(options =>
{
    foreach (var certificate in options.Certificates)
    {
        var sampleCertificate = certificates.GetByName(certificate.Name ?? string.Empty);
        certificate.CertificatePath = sampleCertificate.CertificatePath;
        certificate.PrivateKeyPath = sampleCertificate.PrivateKeyPath;
        certificate.Source = "PemFiles";
    }
});
builder.Services.AddSingleton(certificates);

builder.Services.AddSingleton<IMockQuoteApiClient, MockQuoteApiClient>();

foreach (var clientProfile in clientProfiles.Profiles)
{
    builder.Services.AddHttpClient(clientProfile.Name, (serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<MockQuoteApiOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseAddress, UriKind.Absolute);
        })
        .AddSignedJwtAuthentication(clientProfile.ApiKey, clientProfile.CertificateName);
}

builder.Services.AddControllersWithViews();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Somewhat.SignedJwt.GoogleSecretManager;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGoogleSecretManagerSigningCertificate(
        this IServiceCollection services,
        string name,
        Action<GoogleSecretManagerCertificateOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(configure);

        services.TryAddEnumerable(ServiceDescriptor.Singleton<ISigningCertificateLoader, GoogleSecretManagerSigningCertificateLoader>());
        return services.AddSigningCertificate(name, options =>
        {
            var googleOptions = new GoogleSecretManagerCertificateOptions();
            configure(googleOptions);

            options.Source = GoogleSecretManagerSigningCertificateLoader.LoaderSourceName;
            options.Password = googleOptions.Password;
            options.Parameters[GoogleSecretManagerSigningCertificateLoader.ProjectIdParameter] = googleOptions.ProjectId;
            options.Parameters[GoogleSecretManagerSigningCertificateLoader.SecretIdParameter] = googleOptions.SecretId;
            options.Parameters[GoogleSecretManagerSigningCertificateLoader.SecretVersionParameter] = googleOptions.SecretVersion;
            options.Parameters[GoogleSecretManagerSigningCertificateLoader.PayloadFormatParameter] = googleOptions.PayloadFormat;
        });
    }

    public static IServiceCollection AddGoogleSecretManagerSigningCertificate(
        this IServiceCollection services,
        string name,
        IConfigurationSection section)
    {
        ArgumentNullException.ThrowIfNull(section);

        return services.AddGoogleSecretManagerSigningCertificate(name, options => section.Bind(options));
    }
}
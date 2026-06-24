using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Somewhat.SignedJwt.AwsCertificateManager;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAwsCertificateManagerSigningCertificate(
        this IServiceCollection services,
        string name,
        Action<AwsCertificateManagerOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(configure);

        services.TryAddEnumerable(ServiceDescriptor.Singleton<ISigningCertificateLoader, AwsCertificateManagerSigningCertificateLoader>());
        return services.AddSigningCertificate(name, options =>
        {
            var awsOptions = new AwsCertificateManagerOptions();
            configure(awsOptions);

            options.Source = AwsCertificateManagerSigningCertificateLoader.LoaderSourceName;
            options.Parameters[AwsCertificateManagerSigningCertificateLoader.CertificateArnParameter] = awsOptions.CertificateArn;
            options.Parameters[AwsCertificateManagerSigningCertificateLoader.RegionSystemNameParameter] = awsOptions.RegionSystemName;
            options.Parameters[AwsCertificateManagerSigningCertificateLoader.ExportPassphraseParameter] = awsOptions.ExportPassphrase;
        });
    }

    public static IServiceCollection AddAwsCertificateManagerSigningCertificate(
        this IServiceCollection services,
        string name,
        IConfigurationSection section)
    {
        ArgumentNullException.ThrowIfNull(section);

        return services.AddAwsCertificateManagerSigningCertificate(name, options => section.Bind(options));
    }
}
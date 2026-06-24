using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Somewhat.SignedJwt.AzureKeyVault;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAzureKeyVaultSigningCertificate(
        this IServiceCollection services,
        string name,
        Action<AzureKeyVaultCertificateOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(configure);

        services.TryAddEnumerable(ServiceDescriptor.Singleton<ISigningCertificateLoader, AzureKeyVaultSigningCertificateLoader>());
        return services.AddSigningCertificate(name, options =>
        {
            var keyVaultOptions = new AzureKeyVaultCertificateOptions();
            configure(keyVaultOptions);

            options.Source = AzureKeyVaultSigningCertificateLoader.LoaderSourceName;
            options.Password = keyVaultOptions.Password;
            options.Parameters[AzureKeyVaultSigningCertificateLoader.VaultUriParameter] = keyVaultOptions.VaultUri;
            options.Parameters[AzureKeyVaultSigningCertificateLoader.RetrievalModeParameter] = keyVaultOptions.RetrievalMode;

            if (!string.IsNullOrWhiteSpace(keyVaultOptions.CertificateName))
            {
                options.Parameters[AzureKeyVaultSigningCertificateLoader.CertificateNameParameter] = keyVaultOptions.CertificateName;
            }

            if (!string.IsNullOrWhiteSpace(keyVaultOptions.CertificateVersion))
            {
                options.Parameters[AzureKeyVaultSigningCertificateLoader.CertificateVersionParameter] = keyVaultOptions.CertificateVersion;
            }

            if (!string.IsNullOrWhiteSpace(keyVaultOptions.SecretName))
            {
                options.Parameters[AzureKeyVaultSigningCertificateLoader.SecretNameParameter] = keyVaultOptions.SecretName;
            }

            if (!string.IsNullOrWhiteSpace(keyVaultOptions.SecretVersion))
            {
                options.Parameters[AzureKeyVaultSigningCertificateLoader.SecretVersionParameter] = keyVaultOptions.SecretVersion;
            }
        });
    }

    public static IServiceCollection AddAzureKeyVaultSigningCertificate(
        this IServiceCollection services,
        string name,
        IConfigurationSection section)
    {
        ArgumentNullException.ThrowIfNull(section);

        return services.AddAzureKeyVaultSigningCertificate(name, options => section.Bind(options));
    }
}
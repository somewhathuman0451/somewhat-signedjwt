using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Somewhat.SignedJwt;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSignedJwtSupport(
        this IServiceCollection services,
        Action<SignedJwtClientOptions>? configureClient = null,
        Action<CertificateSourceOptions>? configureCertificate = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (configureClient is not null)
        {
            services.Configure(configureClient);
        }

        if (configureCertificate is not null)
        {
            services.AddSigningCertificate(DefaultCertificateName, configureCertificate);
        }

        services.AddCoreSignedJwtServices();
        return services;
    }

    public static IServiceCollection AddSignedJwtSupport(
        this IServiceCollection services,
        IConfigurationSection clientSection,
        IConfigurationSection certificateSection)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(clientSection);
        ArgumentNullException.ThrowIfNull(certificateSection);

        services.Configure<SignedJwtClientOptions>(clientSection);
        if (certificateSection.GetChildren().Any(child => string.Equals(child.Key, "Certificates", StringComparison.OrdinalIgnoreCase)))
        {
            services.Configure<SigningCertificateRegistryOptions>(certificateSection);
        }
        else
        {
            services.AddSigningCertificate(DefaultCertificateName, certificateSection);
        }

        services.AddCoreSignedJwtServices();
        return services;
    }

    public static IHttpClientBuilder AddSignedJwtAuthentication(this IHttpClientBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddHttpMessageHandler<SignedJwtAuthenticationHandler>();
    }

    public static IHttpClientBuilder AddSignedJwtAuthentication(
        this IHttpClientBuilder builder,
        string apiKey,
        string? certificateName = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.AddSignedJwtAuthentication(options =>
        {
            options.ApiKey = apiKey;
            options.CertificateName = certificateName;
        });
    }

    public static IHttpClientBuilder AddSignedJwtAuthentication(
        this IHttpClientBuilder builder,
        Action<SignedJwtAuthenticationOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        return builder.AddHttpMessageHandler(serviceProvider =>
        {
            var options = new SignedJwtAuthenticationOptions();
            configure(options);

            return new SignedJwtAuthenticationHandler(
                serviceProvider.GetRequiredService<IJwtTokenFactory>(),
                options);
        });
    }

    public static IServiceCollection AddSigningCertificate(
        this IServiceCollection services,
        string name,
        Action<CertificateSourceOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(configure);

        services.Configure<SigningCertificateRegistryOptions>(options =>
        {
            var certificateOptions = new CertificateSourceOptions { Name = name };
            configure(certificateOptions);
            certificateOptions.Name = string.IsNullOrWhiteSpace(certificateOptions.Name) ? name : certificateOptions.Name;

            AddOrReplaceCertificate(options, certificateOptions);

            if (string.IsNullOrWhiteSpace(options.DefaultCertificateName))
            {
                options.DefaultCertificateName = certificateOptions.Name;
            }
        });

        return services;
    }

    public static IServiceCollection AddSigningCertificate(
        this IServiceCollection services,
        string name,
        IConfigurationSection certificateSection)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(certificateSection);

        return services.AddSigningCertificate(name, options =>
        {
            certificateSection.Bind(options);
            options.Name = string.IsNullOrWhiteSpace(options.Name) ? name : options.Name;
        });
    }

    private const string DefaultCertificateName = "default";

    private static IServiceCollection AddCoreSignedJwtServices(this IServiceCollection services)
    {
        services.TryAddSingleton<ISigningCertificateSource, ConfiguredSigningCertificateSource>();
        services.TryAddSingleton<IJwtTokenFactory, JwtTokenFactory>();
        services.TryAddTransient<SignedJwtAuthenticationHandler>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ISigningCertificateLoader, PemFilesSigningCertificateLoader>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ISigningCertificateLoader, PfxFileSigningCertificateLoader>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ISigningCertificateLoader, InlinePemSigningCertificateLoader>());
        return services;
    }

    private static void AddOrReplaceCertificate(
        SigningCertificateRegistryOptions options,
        CertificateSourceOptions certificateOptions)
    {
        var existingIndex = options.Certificates.FindIndex(existing =>
            string.Equals(existing.Name, certificateOptions.Name, StringComparison.OrdinalIgnoreCase));

        if (existingIndex >= 0)
        {
            options.Certificates[existingIndex] = certificateOptions;
            return;
        }

        options.Certificates.Add(certificateOptions);
    }
}
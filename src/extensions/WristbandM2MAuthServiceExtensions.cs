using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Wristband.AspNet.Auth.M2M;

/// <summary>
/// Provides extension methods for configuring Wristband M2M auth services.
/// </summary>
public static class WristbandM2MAuthServiceExtensions
{
    /// <summary>
    /// Registers a default Wristband M2M authentication service.
    /// </summary>
    /// <param name="services">The service collection to which the authentication services are added.</param>
    /// <param name="configureOptions">A delegate to configure <see cref="WristbandM2MAuthOptions"/>.</param>
    /// <param name="httpClientFactory">Optional external HTTP client factory.</param>
    /// <returns>The modified <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddWristbandM2MAuth(
        this IServiceCollection services,
        Action<WristbandM2MAuthOptions> configureOptions,
        IHttpClientFactory? httpClientFactory = null)
    {
        // Configure the default options
        services.Configure(configureOptions);

        // Register the default client
        services.AddSingleton<IWristbandM2MAuthService>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<WristbandM2MAuthOptions>>();

            // Use explicitly provided factory or try to get from DI; null will trigger fallback to internal factory
            var factory = httpClientFactory ?? sp.GetService<IHttpClientFactory>();
            return new WristbandM2MAuthService(options, factory);
        });

        // Register the client factory to retrieve named clients
        services.TryAddSingleton<WristbandM2MAuthServiceFactory>();

        return services;
    }

    /// <summary>
    /// Registers a named Wristband M2M authentication service.
    /// </summary>
    /// <param name="services">The service collection to which the authentication services are added.</param>
    /// <param name="name">The name of the client configuration.</param>
    /// <param name="configureOptions">A delegate to configure <see cref="WristbandM2MAuthOptions"/>.</param>
    /// <param name="httpClientFactory">Optional external HTTP client factory.</param>
    /// <returns>The modified <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddWristbandM2MAuth(
        this IServiceCollection services,
        string name,
        Action<WristbandM2MAuthOptions> configureOptions,
        IHttpClientFactory? httpClientFactory = null)
    {
        // Configure the named options
        services.Configure(name, configureOptions);

        // Register the named client
        services.AddSingleton(sp =>
        {
            var optionsMonitor = sp.GetRequiredService<IOptionsMonitor<WristbandM2MAuthOptions>>();
            var options = new OptionsWrapper<WristbandM2MAuthOptions>(optionsMonitor.Get(name));

            // Create the named client with its specific factory if provided
            var factory = httpClientFactory ?? sp.GetService<IHttpClientFactory>();
            return new NamedWristbandM2MAuthService(name, options, factory);
        });

        // Register the client factory if not already registered
        services.TryAddSingleton<WristbandM2MAuthServiceFactory>();

        return services;
    }
}

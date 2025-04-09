using Microsoft.Extensions.DependencyInjection;

namespace Wristband.AspNet.Auth.M2M;

/// <summary>
/// Factory for retrieving named Wristband M2M authentication services.
/// </summary>
public class WristbandM2MAuthServiceFactory
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="WristbandM2MAuthServiceFactory"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    public WristbandM2MAuthServiceFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>
    /// Gets a named Wristband M2M authentication service.
    /// </summary>
    /// <param name="name">The name of the client.</param>
    /// <returns>The named Wristband M2M authentication service.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no client with the specified name is registered.</exception>
    public IWristbandM2MAuthService GetService(string name)
    {
        // Get all clients
        var services = _serviceProvider.GetServices<NamedWristbandM2MAuthService>();

        // Find the one with the matching name
        var service = services.FirstOrDefault(s => s.Name == name);

        if (service == null)
        {
            throw new InvalidOperationException($"No M2M auth service registered with name '{name}'");
        }

        return service;
    }
}

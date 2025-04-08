using Microsoft.Extensions.Options;

namespace Wristband.AspNet.Auth.M2M;

/// <summary>
/// Named implementation of the Wristband M2M authentication service.
/// </summary>
public class NamedWristbandM2MAuthService : WristbandM2MAuthService
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NamedWristbandM2MAuthService"/> class.
    /// </summary>
    /// <param name="name">The name of the client.</param>
    /// <param name="options">The options for this client.</param>
    /// <param name="httpClientFactory">Optional HTTP client factory.</param>
    public NamedWristbandM2MAuthService(
        string name,
        IOptions<WristbandM2MAuthOptions> options,
        IHttpClientFactory? httpClientFactory = null)
        : base(options, httpClientFactory)
    {
        Name = name;
    }

    /// <summary>
    /// Gets the name of this client instance.
    /// </summary>
    public string Name { get; }
}

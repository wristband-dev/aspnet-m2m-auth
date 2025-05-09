namespace Wristband.AspNet.Auth.M2M;

/// <summary>
/// Configuration options for machine-to-machine (M2M) authentication in Wristband.
/// </summary>
public class WristbandM2MAuthOptions
{
    /// <summary>
    /// Gets or sets the Wristband application vanity domain used for authentication requests.
    /// </summary>
    public string? WristbandApplicationDomain { get; set; }

    /// <summary>
    /// Gets or sets the client ID of the Wristband M2M OAuth2 Client.
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// Gets or sets the client secret of the Wristband M2M OAuth2 Client.
    /// </summary>
    public string? ClientSecret { get; set; }

    /// <summary>
    /// Gets or sets specifies how often the background process should refresh the access token.
    /// If not set, background refreshing is disabled by default.
    /// The minimum allowed interval is 1 minute to prevent excessive requests.
    /// </summary>
    public TimeSpan? BackgroundTokenRefreshInterval { get; set; }

    /// <summary>
    /// Gets or sets optional buffer time to subtract from the token's expiration time.
    /// This helps ensure tokens are refreshed before they actually expire.
    /// Default is 60 seconds. Minimum value is TimeSpan.Zero.
    /// </summary>
    public TimeSpan TokenExpiryBuffer { get; set; } = TimeSpan.FromSeconds(60);
}

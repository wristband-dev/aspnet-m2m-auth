namespace Wristband.AspNet.Auth.M2M;

/// <summary>
/// Interface for Wristband machine-to-machine authentication services.
/// Provides methods to retrieve and manage access tokens for M2M authentication.
/// </summary>
public interface IWristbandM2MAuthService : IDisposable
{
    /// <summary>
    /// Retrieves an access token for machine-to-machine authentication.
    /// </summary>
    /// <returns>A task that resolves to a valid access token string.</returns>
    Task<string> GetTokenAsync();

    /// <summary>
    /// Clears the currently cached access token.
    /// </summary>
    void ClearToken();
}

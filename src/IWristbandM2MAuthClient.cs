namespace Wristband.AspNet.Auth.M2M;

/// <summary>
/// Interface for functionality around making REST API calls to the Wristband platform.
/// </summary>
internal interface IWristbandM2MAuthClient
{
    /// <summary>
    /// Calls the Wristband Token Endpoint with the client credentials grant type to get an access token.
    /// </summary>
    /// <returns>A <see cref="Task{TokenResponse}"/> representing the asynchronous operation. The result contains the access token.</returns>
    /// <remarks><a href="https://docs.wristband.dev/reference/tokenv1">Wristband Token Endpoint</a></remarks>
    Task<TokenResponse> GetM2MToken();
}

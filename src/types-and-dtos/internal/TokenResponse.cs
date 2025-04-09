using System.Text.Json.Serialization;

namespace Wristband.AspNet.Auth.M2M;

/// <summary>
/// Represents the token response received from the Wristband Token Endpoint.
/// </summary>
internal class TokenResponse
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TokenResponse"/> class.
    /// </summary>
    public TokenResponse()
    {
    }

    /// <summary>
    /// Gets or sets the access token.
    /// </summary>
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the expiration time of the access token (in seconds).
    /// </summary>
    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; } = 0;

    /// <summary>
    /// Gets or sets the type of token.
    /// </summary>
    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = string.Empty;
}

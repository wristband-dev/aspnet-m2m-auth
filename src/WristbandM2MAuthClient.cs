using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Wristband.AspNet.Auth.M2M;

/// <summary>
/// Factory for creating and configuring HTTP clients for Wristband authentication.
/// </summary>
internal class WristbandM2MAuthClient : IWristbandM2MAuthClient
{
    // Default timeout for HTTP requests in seconds
    private const int DefaultTimeoutSeconds = 30;

    // Reusable form content for client credentials grant type requests
    private static readonly FormUrlEncodedContent ClientCredentialsContent = new(
        new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>("grant_type", "client_credentials"),
        });

    // Lazy-initialized HTTP client factory instance
    private static readonly Lazy<IHttpClientFactory> _internalFactory = new Lazy<IHttpClientFactory>(() =>
        CreateInternalFactory());

    // The HTTP client for making requests
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="WristbandM2MAuthClient"/> class.
    /// </summary>
    /// <param name="options">The options for configuring the client.</param>
    /// <param name="externalFactory">Optional external HTTP client factory. If not provided, an internal factory will be used.</param>
    /// <exception cref="ArgumentNullException">Thrown when options is null.</exception>
    /// <exception cref="ArgumentException">Thrown when required options are missing or invalid.</exception>
    internal WristbandM2MAuthClient(IOptions<WristbandM2MAuthOptions> options, IHttpClientFactory? externalFactory = null)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        var optionsValue = options.Value ?? throw new ArgumentNullException(nameof(options));

        if (string.IsNullOrEmpty(optionsValue.WristbandApplicationDomain))
        {
            throw new ArgumentException("WristbandApplicationDomain is required");
        }

        if (string.IsNullOrEmpty(optionsValue.ClientId))
        {
            throw new ArgumentException("ClientId is required");
        }

        if (string.IsNullOrEmpty(optionsValue.ClientSecret))
        {
            throw new ArgumentException("ClientSecret is required");
        }

        // Use the provided factory, or fall back to internal one
        var factory = externalFactory ?? _internalFactory.Value;

        // Create and configure the client
        _httpClient = factory.CreateClient("WristbandM2MAuth");
        _httpClient.BaseAddress = new Uri($"https://{optionsValue.WristbandApplicationDomain}");
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.Timeout = TimeSpan.FromSeconds(DefaultTimeoutSeconds);

        // Add a Basic authentication header
        var credentialsBytes = Encoding.UTF8.GetBytes($"{optionsValue.ClientId}:{optionsValue.ClientSecret}");
        var base64Credentials = Convert.ToBase64String(credentialsBytes);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64Credentials);
    }

    /// <summary>
    /// Requests a token from the Wristband token endpoint.
    /// </summary>
    /// <returns>A token response containing the access token and expiration information.</returns>
    /// <exception cref="HttpRequestException">Thrown when the HTTP request fails.</exception>
    /// <exception cref="Exception">Thrown when the response cannot be deserialized.</exception>
    public async Task<TokenResponse> GetM2MToken()
    {
        // Create and send token request
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/oauth2/token");
        request.Content = ClientCredentialsContent;

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>()
            ?? throw new InvalidOperationException("Failed to deserialize token response");

        return tokenResponse;
    }

    /// <summary>
    /// Creates an internal HTTP client factory for token requests.
    /// This allows the class to create and configure HTTP clients without external dependencies.
    /// </summary>
    /// <returns>An HTTP client factory configured for Wristband token requests.</returns>
    private static IHttpClientFactory CreateInternalFactory()
    {
        var services = new ServiceCollection();
        services.AddHttpClient("WristbandM2MAuth", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(DefaultTimeoutSeconds);
        });
        var serviceProvider = services.BuildServiceProvider();
        return serviceProvider.GetRequiredService<IHttpClientFactory>();
    }
}

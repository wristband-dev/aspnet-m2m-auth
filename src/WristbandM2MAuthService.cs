using System.Net;

using Microsoft.Extensions.Options;

namespace Wristband.AspNet.Auth.M2M;

/// <summary>
/// Provides token acquisition, caching, automatic refresh, and background refresh for Wristband M2M authentication.
/// </summary>
public class WristbandM2MAuthService : IWristbandM2MAuthService
{
    // Maximum number of attempts to refresh a token before failing.
    private const int MaxRefreshAttempts = 3;

    // Delay in milliseconds between token refresh retry attempts.
    private const int RetryDelayMs = 100;

    // HTTP client for making requests to Wristband.
    private readonly IWristbandM2MAuthClient _authClient;

    // Semaphore to prevent concurrent token refresh operations.
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

    // Cancellation token source for the background refresh task.
    private readonly CancellationTokenSource _cts = new();

    // Background task that periodically refreshes the token, if configured.
    private readonly Task? _backgroundRefreshTask;

    // Buffer time to subtract from the token's expiration time.
    private readonly TimeSpan _tokenExpiryBuffer;

    // The currently cached access token; empty when no valid token exists.
    private string _cachedToken = string.Empty;

    // Expiration time of the currently cached token in UTC.
    private DateTime _tokenExpiry = DateTime.MinValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="WristbandM2MAuthService"/> class with the specified options.
    /// Sets up HTTP client with authentication headers and starts a background token refresh task, if configured.
    /// </summary>
    /// <param name="options">The options for configuring the Wristband M2M authentication service.</param>
    /// <param name="httpClientFactory">Optional external HTTP client factory. If not provided, an internal factory will be used.</param>
    /// <exception cref="ArgumentNullException">Thrown when options is null.</exception>
    /// <exception cref="ArgumentException">Thrown when required options are missing or invalid.</exception>
    public WristbandM2MAuthService(IOptions<WristbandM2MAuthOptions> options, IHttpClientFactory? httpClientFactory = null)
    {
        // Validate configuration options.
        var optionsValue = options.Value ?? throw new ArgumentNullException(nameof(options));

        if (optionsValue.BackgroundTokenRefreshInterval.HasValue
                && optionsValue.BackgroundTokenRefreshInterval < TimeSpan.FromMinutes(1))
        {
            throw new ArgumentException("BackgroundTokenRefreshInterval must be at least 1 minute");
        }

        if (optionsValue.TokenExpiryBuffer < TimeSpan.Zero)
        {
            throw new ArgumentException("TokenExpiryBuffer cannot be negative");
        }

        _tokenExpiryBuffer = optionsValue.TokenExpiryBuffer;

        // Create a token client using the factory - validation happens inside the factory
        _authClient = new WristbandM2MAuthClient(options, httpClientFactory);

        // If a valid BackgroundTokenRefreshInterval is provided, start the background refresh loop.
        if (optionsValue.BackgroundTokenRefreshInterval.HasValue)
        {
            // Start background refresh loop with the configured interval
            _backgroundRefreshTask = Task.Run(() => BackgroundTokenRefreshLoop(optionsValue.BackgroundTokenRefreshInterval.Value), _cts.Token);
        }
    }

    /// <summary>
    /// Retrieves an access token for the machine-to-machine client. The access token is cached in memory for subsequent calls.
    /// When the access token expires, this method will automatically get a new access token when it's called.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task<string> GetTokenAsync()
    {
        await RefreshTokenAsync();
        return _cachedToken;
    }

    /// <summary>
    /// Clears the access token from the cache.
    /// </summary>
    public void ClearToken()
    {
        _cachedToken = string.Empty;
        _tokenExpiry = DateTime.MinValue;
    }

    /// <summary>
    /// Disposes resources used by this instance, including canceling any background refresh tasks.
    /// </summary>
    public void Dispose()
    {
        // Cancel the token source
        _cts.Cancel();

        try
        {
            // If the background task was created, wait for it to complete
            _backgroundRefreshTask?.Wait();
        }
        catch (AggregateException ex)
        {
            // Handle the exception if it's TaskCanceledException or OperationCanceledException
            if (ex.InnerExceptions.Any(innerEx => innerEx is TaskCanceledException || innerEx is OperationCanceledException))
            {
                // This can happen due to Dispose cancellation, but it's expected.
            }
            else
            {
                // Rethrow if it's some other exception.
                throw;
            }
        }

        // Clean up any other resources if necessary
        _cts.Dispose();
    }

    /// <summary>
    /// Refreshes the access token if needed with retry logic for server errors.
    /// Makes up to MaxRefreshAttempts to refresh the token with RetryDelayMs delay between attempts.
    /// Only retries on 5xx server errors, fails immediately on 4xx client errors.
    /// </summary>
    /// <returns>A task that completes when the token has been refreshed or determined to be valid.</returns>
    /// <exception cref="Exception">Thrown when all refresh attempts fail or when a client error (4xx) occurs.</exception>
    private async Task RefreshTokenAsync()
    {
        // Check if we have a valid cached token
        if (IsTokenValid())
        {
            return;
        }

        // Use semaphore to prevent multiple concurrent token requests
        await _semaphore.WaitAsync();

        try
        {
            // Double-check if token was refreshed by another thread
            if (IsTokenValid())
            {
                return;
            }

            // Try up to 3 times to get a new token.
            for (int attempt = 1; attempt <= MaxRefreshAttempts; attempt++)
            {
                try
                {
                    var tokenResponse = await _authClient.GetM2MToken();

                    // Cache the token and set expiry based on configured buffer
                    var buffer = tokenResponse.ExpiresIn <= _tokenExpiryBuffer.TotalSeconds ? TimeSpan.FromMinutes(1) : _tokenExpiryBuffer;
                    _tokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn).Subtract(buffer);
                    _cachedToken = tokenResponse.AccessToken;
                    return;
                }
                catch (HttpRequestException ex) when (ex.StatusCode >= HttpStatusCode.InternalServerError)
                {
                    // Only retry for 5xx server errors and if we haven't reached max attempts
                    if (attempt == MaxRefreshAttempts)
                    {
                        throw;
                    }

                    await Task.Delay(RetryDelayMs);
                }
                catch (HttpRequestException ex) when (ex.StatusCode >= HttpStatusCode.BadRequest && ex.StatusCode < HttpStatusCode.InternalServerError)
                {
                    // Don't retry for 4xx client errors
                    throw;
                }
                catch
                {
                    // Don't retry for unexpected error
                    throw;
                }
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Checks if the current token is valid (not empty and not expired).
    /// </summary>
    /// <returns>True if the cached token is valid and not expired, otherwise false.</returns>
    private bool IsTokenValid()
    {
        return !string.IsNullOrEmpty(_cachedToken) && DateTime.UtcNow < _tokenExpiry;
    }

    /// <summary>
    /// Background task that periodically refreshes the access token at the specified interval.
    /// Continues running until the service is disposed or an unrecoverable error occurs.
    /// </summary>
    /// <param name="refreshInterval">The interval at which to refresh the token.</param>
    /// <returns>A task that completes when the cancellation token is canceled.</returns>
    private async Task BackgroundTokenRefreshLoop(TimeSpan refreshInterval)
    {
        while (!_cts.Token.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(refreshInterval, _cts.Token);
                await RefreshTokenAsync();
            }
            catch (Exception ex) when (ex is TaskCanceledException || ex is OperationCanceledException)
            {
                // Exit loop cleanly on shutdown
                break;
            }
            catch
            {
                // Unexpected error
                throw;
            }
        }
    }
}

using System.Net;

using Microsoft.Extensions.Options;

using Moq;

namespace Wristband.AspNet.Auth.M2M.Tests;

public class WristbandM2MAuthServiceTests
{
    private IOptions<WristbandM2MAuthOptions> CreateValidOptions()
    {
        return Options.Create(new WristbandM2MAuthOptions
        {
            WristbandApplicationDomain = "test.wristband.com",
            ClientId = "test-client-id",
            ClientSecret = "test-client-secret",
            TokenExpiryBuffer = TimeSpan.FromSeconds(60),
            BackgroundTokenRefreshInterval = TimeSpan.FromMinutes(1)
        });
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenOptionsIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new WristbandM2MAuthService(Options.Create<WristbandM2MAuthOptions>(null!))
        );
    }

    [Fact]
    public void Constructor_ThrowsArgumentException_WhenBackgroundTokenRefreshIntervalIsTooShort()
    {
        var options = new WristbandM2MAuthOptions
        {
            WristbandApplicationDomain = "test.wristband.com",
            ClientId = "test-client-id",
            ClientSecret = "test-client-secret",
            TokenExpiryBuffer = TimeSpan.FromSeconds(60),
            BackgroundTokenRefreshInterval = TimeSpan.FromSeconds(30),
        };

        Assert.Throws<ArgumentException>(() => new WristbandM2MAuthService(Options.Create(options)));
    }

    [Fact]
    public void Constructor_ThrowsArgumentException_WhenTokenExpiryBufferIsNegative()
    {
        var options = new WristbandM2MAuthOptions
        {
            WristbandApplicationDomain = "test.wristband.com",
            ClientId = "test-client-id",
            ClientSecret = "test-client-secret",
            TokenExpiryBuffer = TimeSpan.FromSeconds(-1),
            BackgroundTokenRefreshInterval = TimeSpan.FromMinutes(1)
        };

        Assert.Throws<ArgumentException>(() => new WristbandM2MAuthService(Options.Create(options)));
    }

    [Fact]
    public async Task GetTokenAsync_CallsTokenClient_GetM2MToken_AndCachesToken()
    {
        // Mock the client
        var mockClient = new Mock<IWristbandApiClient>();
        mockClient
            .Setup(c => c.GetM2MToken())
            .ReturnsAsync(new TokenResponse
            {
                AccessToken = "new-access-token",
                ExpiresIn = 3600,
                TokenType = "bearer",
            });
        var service = new WristbandM2MAuthService(CreateValidOptions());

        // Use reflection to inject the mock client
        var clientField = typeof(WristbandM2MAuthService)
            .GetField("_wristbandApiClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        clientField?.SetValue(service, mockClient.Object);
        var semaphoreField = typeof(WristbandM2MAuthService)
            .GetField("_semaphore", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var semaphore = semaphoreField?.GetValue(service) as SemaphoreSlim;

        var token = await service.GetTokenAsync();

        Assert.Equal("new-access-token", token);
        mockClient.Verify(c => c.GetM2MToken(), Times.Once);
        // The semaphore should be fully released (CurrentCount == 1)
        Assert.Equal(1, semaphore?.CurrentCount);
    }

    [Fact]
    public async Task GetTokenAsync_ReturnsCachedToken_WhenTokenIsAlreadyValid()
    {
        var options = CreateValidOptions();
        var mockClient = new Mock<IWristbandApiClient>();
        var service = new WristbandM2MAuthService(options);

        // Use reflection to set a valid cached token
        var tokenField = typeof(WristbandM2MAuthService)
            .GetField("_cachedToken", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var expiryField = typeof(WristbandM2MAuthService)
            .GetField("_tokenExpiry", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var semaphoreField = typeof(WristbandM2MAuthService)
            .GetField("_semaphore", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var semaphore = semaphoreField?.GetValue(service) as SemaphoreSlim;

        tokenField?.SetValue(service, "cached-token");
        expiryField?.SetValue(service, DateTime.UtcNow.AddHours(1));

        // Use reflection to inject the mock client
        var clientField = typeof(WristbandM2MAuthService)
            .GetField("_wristbandApiClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        clientField?.SetValue(service, mockClient.Object);

        var token = await service.GetTokenAsync();

        Assert.Equal("cached-token", token);
        mockClient.Verify(c => c.GetM2MToken(), Times.Never);
        Assert.Equal(1, semaphore?.CurrentCount);
    }

    [Fact]
    public void ClearToken_ResetsTokenAndExpiry()
    {
        var service = new WristbandM2MAuthService(CreateValidOptions());

        // Use reflection to set a valid cached token
        var tokenField = typeof(WristbandM2MAuthService)
            .GetField("_cachedToken", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var expiryField = typeof(WristbandM2MAuthService)
            .GetField("_tokenExpiry", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        tokenField?.SetValue(service, "test-token");
        expiryField?.SetValue(service, DateTime.UtcNow.AddHours(1));

        service.ClearToken();

        // Assert via reflection
        var clearedToken = tokenField?.GetValue(service) as string;
        var clearedExpiry = (DateTime)expiryField?.GetValue(service)!;
        Assert.Equal(string.Empty, clearedToken);
        Assert.Equal(DateTime.MinValue, clearedExpiry);
    }

    [Fact]
    public async Task GetTokenAsync_RetriesOnServerError()
    {
        // Setup to throw server errors on first two attempts, then succeed
        var mockClient = new Mock<IWristbandApiClient>();
        mockClient
            .SetupSequence(c => c.GetM2MToken())
            .ThrowsAsync(new HttpRequestException("Server Error 1", null, HttpStatusCode.InternalServerError))
            .ThrowsAsync(new HttpRequestException("Server Error 2", null, HttpStatusCode.ServiceUnavailable))
            .ReturnsAsync(new TokenResponse
            {
                AccessToken = "retry-success-token",
                ExpiresIn = 3600,
                TokenType = "bearer"
            });
        var service = new WristbandM2MAuthService(CreateValidOptions());

        // Use reflection to inject the mock client
        var clientField = typeof(WristbandM2MAuthService)
            .GetField("_wristbandApiClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        clientField?.SetValue(service, mockClient.Object);
        var semaphoreField = typeof(WristbandM2MAuthService)
            .GetField("_semaphore", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var semaphore = semaphoreField?.GetValue(service) as SemaphoreSlim;

        var token = await service.GetTokenAsync();

        Assert.Equal("retry-success-token", token);
        mockClient.Verify(c => c.GetM2MToken(), Times.Exactly(3));
        Assert.Equal(1, semaphore?.CurrentCount);
    }

    [Fact]
    public async Task GetTokenAsync_ThrowsOnClientError()
    {
        var mockClient = new Mock<IWristbandApiClient>();
        mockClient
            .Setup(c => c.GetM2MToken())
            .ThrowsAsync(new HttpRequestException("Bad Request", null, HttpStatusCode.BadRequest));
        var service = new WristbandM2MAuthService(CreateValidOptions());

        // Use reflection to inject the mock client
        var clientField = typeof(WristbandM2MAuthService)
            .GetField("_wristbandApiClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        clientField?.SetValue(service, mockClient.Object);
        var semaphoreField = typeof(WristbandM2MAuthService)
            .GetField("_semaphore", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var semaphore = semaphoreField?.GetValue(service) as SemaphoreSlim;

        await Assert.ThrowsAsync<HttpRequestException>(() => service.GetTokenAsync());
        Assert.Equal(1, semaphore?.CurrentCount);
    }

    [Fact]
    public void Dispose_CancelsCancellationTokenSource()
    {
        var service = new WristbandM2MAuthService(CreateValidOptions());

        // Use reflection to get the cancellation token source
        var ctsField = typeof(WristbandM2MAuthService)
            .GetField("_cts", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var cts = ctsField?.GetValue(service) as CancellationTokenSource;

        service.Dispose();

        Assert.True(cts?.IsCancellationRequested);
    }
}

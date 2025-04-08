using System.Net;
using System.Text;

using Microsoft.Extensions.Options;

using Moq;
using Moq.Protected;

namespace Wristband.AspNet.Auth.M2M.Tests;

public class WristbandM2MAuthClientTests
{
    private IOptions<WristbandM2MAuthOptions> CreateValidOptions()
    {
        return Options.Create(new WristbandM2MAuthOptions
        {
            WristbandApplicationDomain = "test.wristband.com",
            ClientId = "test-client-id",
            ClientSecret = "test-client-secret"
        });
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenOptionsIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new WristbandM2MAuthClient(null!));
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenOptionsValueIsNull()
    {
        // Create a mock IOptions<WristbandM2MAuthOptions> with a null Value
        var mockOptions = new Mock<IOptions<WristbandM2MAuthOptions>>();
        mockOptions.Setup(o => o.Value).Returns((WristbandM2MAuthOptions)null!);

        Assert.Throws<ArgumentNullException>(() =>
            new WristbandM2MAuthClient(mockOptions.Object, null));
    }

    [Theory]
    [InlineData("", "client-id", "secret", "WristbandApplicationDomain")]
    [InlineData("domain", "", "secret", "ClientId")]
    [InlineData("domain", "client-id", "", "ClientSecret")]
    public void Constructor_ThrowsArgumentException_WhenRequiredOptionsAreMissing(
        string domain, string clientId, string clientSecret, string expectedParamName)
    {
        var options = Options.Create(new WristbandM2MAuthOptions
        {
            WristbandApplicationDomain = domain,
            ClientId = clientId,
            ClientSecret = clientSecret
        });

        var ex = Assert.Throws<ArgumentException>(() =>
            new WristbandM2MAuthClient(options));

        Assert.Contains(expectedParamName, ex.Message);
    }

    [Fact]
    public void Constructor_SetsCorrectBaseAddress()
    {
        var options = CreateValidOptions();
        var mockFactory = new Mock<IHttpClientFactory>();
        var mockHttpClient = new Mock<HttpClient>();

        mockFactory
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(mockHttpClient.Object);

        var client = new WristbandM2MAuthClient(options, mockFactory.Object);

        // Use reflection to get the HttpClient
        var httpClientField = typeof(WristbandM2MAuthClient)
            .GetField("_httpClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var httpClient = httpClientField?.GetValue(client) as HttpClient;

        Assert.Equal(new Uri("https://test.wristband.com"), httpClient?.BaseAddress);
    }

    [Fact]
    public void Constructor_SetsBasicAuthHeader()
    {
        var options = CreateValidOptions();
        var mockFactory = new Mock<IHttpClientFactory>();
        var mockHttpClient = new Mock<HttpClient>();

        mockFactory
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(mockHttpClient.Object);

        var client = new WristbandM2MAuthClient(options, mockFactory.Object);

        // Use reflection to get the HttpClient
        var httpClientField = typeof(WristbandM2MAuthClient)
            .GetField("_httpClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var httpClient = httpClientField?.GetValue(client) as HttpClient;

        var expectedCredentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes("test-client-id:test-client-secret")
        );

        Assert.Equal("Basic", httpClient?.DefaultRequestHeaders.Authorization?.Scheme);
        Assert.Equal(expectedCredentials, httpClient?.DefaultRequestHeaders.Authorization?.Parameter);
    }

    [Fact]
    public async Task GetM2MToken_SendsCorrectRequest()
    {
        var options = CreateValidOptions();
        var mockFactory = new Mock<IHttpClientFactory>();
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();

        // Setup mock handler to return a successful response
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(
                    req =>
                        req.Method == HttpMethod.Post &&
                        IsValidTokenRequestUri(req.RequestUri) &&
                        req.Content is FormUrlEncodedContent
                ),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(
                    System.Text.Json.JsonSerializer.Serialize(new TokenResponse
                    {
                        AccessToken = "test-token",
                        ExpiresIn = 3600,
                        TokenType = "Bearer"
                    })
                )
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        mockFactory
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);

        var client = new WristbandM2MAuthClient(options, mockFactory.Object);
        var tokenResponse = await client.GetM2MToken();

        Assert.Equal("test-token", tokenResponse.AccessToken);
        Assert.Equal(3600, tokenResponse.ExpiresIn);
        Assert.Equal("Bearer", tokenResponse.TokenType);

        // Verify the request was sent
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(
                    req =>
                        req.Method == HttpMethod.Post &&
                        IsValidTokenRequestUri(req.RequestUri) &&
                        req.Content is FormUrlEncodedContent
                ),
                ItExpr.IsAny<CancellationToken>()
            );
    }

    [Fact]
    public async Task GetM2MToken_ThrowsOnDeserializationFailure()
    {
        var options = CreateValidOptions();
        var mockFactory = new Mock<IHttpClientFactory>();
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();

        // Setup mock handler to return an empty response
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("")
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        mockFactory
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);

        var client = new WristbandM2MAuthClient(options, mockFactory.Object);

        await Assert.ThrowsAsync<System.Text.Json.JsonException>(() => client.GetM2MToken());
    }

    [Fact]
    public void CreateInternalFactory_ReturnsConfiguredHttpClientFactory()
    {
        // Use reflection to call the private static method
        var method = typeof(WristbandM2MAuthClient)
            .GetMethod("CreateInternalFactory", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var factory = method?.Invoke(null, null) as IHttpClientFactory;

        Assert.NotNull(factory);

        // Create a client and verify its configuration
        var client = factory.CreateClient("WristbandM2MAuth");

        Assert.Equal(TimeSpan.FromSeconds(30), client.Timeout);
    }

    private static bool IsValidTokenRequestUri(Uri? uri)
    {
        return uri != null && uri.AbsolutePath == "/api/v1/oauth2/token";
    }
}

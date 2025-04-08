using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Moq;

namespace Wristband.AspNet.Auth.M2M.Tests;

public class WristbandM2MAuthServiceExtensionsTests
{
    [Fact]
    public void AddWristbandM2MAuth_RegistersServices_WithoutExplicitFactory()
    {
        var services = new ServiceCollection();
        services.AddWristbandM2MAuth(options =>
        {
            options.WristbandApplicationDomain = "test.wristband.dev";
            options.ClientId = "test-client-id";
            options.ClientSecret = "test-client-secret";
        });
        var serviceProvider = services.BuildServiceProvider();

        // Should be able to resolve IOptions<WristbandM2MAuthOptions>
        var options = serviceProvider.GetRequiredService<IOptions<WristbandM2MAuthOptions>>();
        Assert.Equal("test.wristband.dev", options.Value.WristbandApplicationDomain);
        Assert.Equal("test-client-id", options.Value.ClientId);
        Assert.Equal("test-client-secret", options.Value.ClientSecret);

        // Should be able to resolve IWristbandM2MAuthService
        var authService = serviceProvider.GetRequiredService<IWristbandM2MAuthService>();
        Assert.IsType<WristbandM2MAuthService>(authService);
    }

    [Fact]
    public void AddWristbandM2MAuth_RegistersServices_WithExplicitFactory()
    {
        var services = new ServiceCollection();
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();

        // Set up the mock to return a HttpClient when CreateClient is called
        httpClientFactoryMock
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient());

        // Add the mock IHttpClientFactory explicitly
        services.AddSingleton(httpClientFactoryMock.Object);

        // Register services with explicit configuration
        services.AddWristbandM2MAuth(options =>
        {
            options.WristbandApplicationDomain = "test.wristband.dev";
            options.ClientId = "test-client-id";
            options.ClientSecret = "test-client-secret";
        }, httpClientFactoryMock.Object);

        var serviceProvider = services.BuildServiceProvider();

        // Ensure IOptions<WristbandM2MAuthOptions> can be resolved
        var options = serviceProvider.GetRequiredService<IOptions<WristbandM2MAuthOptions>>();
        Assert.Equal("test.wristband.dev", options.Value.WristbandApplicationDomain);

        // Ensure IWristbandM2MAuthService can be resolved
        var authService = serviceProvider.GetRequiredService<IWristbandM2MAuthService>();
        Assert.IsAssignableFrom<IWristbandM2MAuthService>(authService);
    }

    [Fact]
    public void AddWristbandM2MAuth_ConfiguresOptions_Correctly()
    {
        var services = new ServiceCollection();
        services.AddWristbandM2MAuth(options =>
        {
            options.WristbandApplicationDomain = "test.wristband.dev";
            options.ClientId = "test-client-id";
            options.ClientSecret = "test-client-secret";
            options.TokenExpiryBuffer = TimeSpan.FromMinutes(10);
            options.BackgroundTokenRefreshInterval = TimeSpan.FromMinutes(30);
        });
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<WristbandM2MAuthOptions>>();

        Assert.Equal("test.wristband.dev", options.Value.WristbandApplicationDomain);
        Assert.Equal("test-client-id", options.Value.ClientId);
        Assert.Equal("test-client-secret", options.Value.ClientSecret);
        Assert.Equal(TimeSpan.FromMinutes(10), options.Value.TokenExpiryBuffer);
        Assert.Equal(TimeSpan.FromMinutes(30), options.Value.BackgroundTokenRefreshInterval);
    }

    [Fact]
    public void AddWristbandM2MAuth_RegistersServiceAsSingleton()
    {
        var services = new ServiceCollection();

        // Register services without passing IHttpClientFactory
        services.AddWristbandM2MAuth(options =>
        {
            options.WristbandApplicationDomain = "test.wristband.dev";
            options.ClientId = "test-client-id";
            options.ClientSecret = "test-client-secret";
        });

        var serviceDescriptor = services.FirstOrDefault(sd =>
            sd.ServiceType == typeof(IWristbandM2MAuthService) &&
            sd.Lifetime == ServiceLifetime.Singleton);

        // Ensure that the service is registered as a Singleton
        Assert.NotNull(serviceDescriptor);
        Assert.Equal(ServiceLifetime.Singleton, serviceDescriptor.Lifetime);

        // Verify the service can be resolved
        var serviceProvider = services.BuildServiceProvider();
        var authService = serviceProvider.GetService<IWristbandM2MAuthService>();
        Assert.NotNull(authService);
    }

    [Fact]
    public void AddWristbandM2MAuth_RegistersServiceAsSingleton_WithFactoryLambda()
    {
        var services = new ServiceCollection();
        services.AddWristbandM2MAuth(options =>
        {
            options.WristbandApplicationDomain = "test.wristband.dev";
            options.ClientId = "test-client-id";
            options.ClientSecret = "test-client-secret";
        });

        var serviceDescriptor = services.FirstOrDefault(sd => sd.ServiceType == typeof(IWristbandM2MAuthService));

        Assert.NotNull(serviceDescriptor);
        Assert.Null(serviceDescriptor!.ImplementationType);
        Assert.Equal(ServiceLifetime.Singleton, serviceDescriptor.Lifetime);
    }

    [Fact]
    public void AddWristbandM2MAuth_Named_RegistersService_AndResolvesCorrectOptions()
    {
        var services = new ServiceCollection();
        services.AddWristbandM2MAuth("client-a", options =>
        {
            options.WristbandApplicationDomain = "client-a.wristband.dev";
            options.ClientId = "client-a-id";
            options.ClientSecret = "client-a-secret";
        });

        var provider = services.BuildServiceProvider();
        var namedClient = provider.GetRequiredService<NamedWristbandM2MAuthService>();

        Assert.Equal("client-a", namedClient.Name);

        // Retrieve the options using IOptionsMonitor
        var options = provider.GetRequiredService<IOptionsMonitor<WristbandM2MAuthOptions>>().Get("client-a");

        Assert.Equal("client-a.wristband.dev", options.WristbandApplicationDomain);
        Assert.Equal("client-a-id", options.ClientId);
        Assert.Equal("client-a-secret", options.ClientSecret);
    }

    [Fact]
    public void AddWristbandM2MAuth_AddsFactorySingletonOnlyOnce()
    {
        var services = new ServiceCollection();

        services.AddWristbandM2MAuth("one", o =>
        {
            o.WristbandApplicationDomain = "one.wristband.dev";
            o.ClientId = "one";
            o.ClientSecret = "one-secret";
        });

        services.AddWristbandM2MAuth("two", o =>
        {
            o.WristbandApplicationDomain = "two.wristband.dev";
            o.ClientId = "two";
            o.ClientSecret = "two-secret";
        });

        var factoryCount = services.Count(sd => sd.ServiceType == typeof(WristbandM2MAuthServiceFactory));

        Assert.Equal(1, factoryCount);
    }

    [Fact]
    public void AddWristbandM2MAuth_Named_UsesProvidedHttpClientFactory()
    {
        var services = new ServiceCollection();

        // Mock IHttpClientFactory
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient());

        // Mock IOptionsMonitor<WristbandM2MAuthOptions>
        var optionsMonitorMock = new Mock<IOptionsMonitor<WristbandM2MAuthOptions>>();
        optionsMonitorMock.Setup(x => x.Get("named-client")).Returns(new WristbandM2MAuthOptions
        {
            WristbandApplicationDomain = "named.wristband.dev",
            ClientId = "named-client-id",
            ClientSecret = "named-client-secret"
        });

        // Register the mocks in DI container
        services.AddSingleton(httpClientFactoryMock.Object);
        services.AddSingleton(optionsMonitorMock.Object);

        // Register your service (NamedWristbandM2MAuthService)
        services.AddWristbandM2MAuth("named-client", options =>
        {
            options.WristbandApplicationDomain = "named.wristband.dev";
            options.ClientId = "named-client-id";
            options.ClientSecret = "named-client-secret";
        }, httpClientFactoryMock.Object);

        var provider = services.BuildServiceProvider();
        var namedClient = provider.GetRequiredService<NamedWristbandM2MAuthService>();

        Assert.Equal("named-client", namedClient.Name);

        // Validate that the IHttpClientFactory mock was called as expected
        httpClientFactoryMock.Verify(x => x.CreateClient(It.IsAny<string>()), Times.Once);

        // Ensure the options were correctly provided
        var options = provider.GetRequiredService<IOptionsMonitor<WristbandM2MAuthOptions>>().Get("named-client");
        Assert.Equal("named.wristband.dev", options.WristbandApplicationDomain);
        Assert.Equal("named-client-id", options.ClientId);
        Assert.Equal("named-client-secret", options.ClientSecret);
    }

    [Fact]
    public void AddWristbandM2MAuth_DefaultAndNamed_DoNotCollide()
    {
        var services = new ServiceCollection();

        // Default registration
        services.AddWristbandM2MAuth(options =>
        {
            options.WristbandApplicationDomain = "default.wristband.dev";
            options.ClientId = "default-id";
            options.ClientSecret = "default-secret";
        });

        // Named registration
        services.AddWristbandM2MAuth("other", options =>
        {
            options.WristbandApplicationDomain = "named.wristband.dev";
            options.ClientId = "named-id";
            options.ClientSecret = "named-secret";
        });

        var provider = services.BuildServiceProvider();

        var defaultClient = provider.GetRequiredService<IWristbandM2MAuthService>();
        var namedClient = provider.GetRequiredService<NamedWristbandM2MAuthService>();

        Assert.NotSame(defaultClient, namedClient);

        // Retrieve the options for the default client using IOptionsMonitor
        var defaultOptions = provider.GetRequiredService<IOptionsMonitor<WristbandM2MAuthOptions>>().Get(Options.DefaultName);
        Assert.Equal("default.wristband.dev", defaultOptions.WristbandApplicationDomain);

        // Retrieve the options for the named client using IOptionsMonitor
        var namedOptions = provider.GetRequiredService<IOptionsMonitor<WristbandM2MAuthOptions>>().Get("other");
        Assert.Equal("named.wristband.dev", namedOptions.WristbandApplicationDomain);
    }
}

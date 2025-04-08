using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Wristband.AspNet.Auth.M2M.Tests;

public class WristbandM2MAuthServiceFactoryTests
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
    public void GetService_WithValidName_ReturnsCorrectService()
    {
        var serviceA = new NamedWristbandM2MAuthService("ServiceA", CreateValidOptions());
        var serviceB = new NamedWristbandM2MAuthService("ServiceB", CreateValidOptions());

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(serviceA);
        serviceCollection.AddSingleton(serviceB);
        var serviceProvider = serviceCollection.BuildServiceProvider();

        var factory = new WristbandM2MAuthServiceFactory(serviceProvider);

        var result = factory.GetService("ServiceA");

        Assert.Same(serviceA, result);
    }

    [Fact]
    public void GetService_WithInvalidName_ThrowsException()
    {
        var serviceA = new NamedWristbandM2MAuthService("ServiceA", CreateValidOptions());

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(serviceA);
        var serviceProvider = serviceCollection.BuildServiceProvider();

        var factory = new WristbandM2MAuthServiceFactory(serviceProvider);

        var exception = Assert.Throws<InvalidOperationException>(() => factory.GetService("NonExistentService"));

        Assert.Contains("No M2M auth service registered with name 'NonExistentService'", exception.Message);
    }

    [Fact]
    public void GetService_WithNoServices_ThrowsException()
    {
        var serviceCollection = new ServiceCollection();
        var serviceProvider = serviceCollection.BuildServiceProvider();

        var factory = new WristbandM2MAuthServiceFactory(serviceProvider);

        var exception = Assert.Throws<InvalidOperationException>(() => factory.GetService("AnyName"));

        Assert.Contains("No M2M auth service registered with name 'AnyName'", exception.Message);
    }

    [Fact]
    public void Constructor_WithNullServiceProvider_ThrowsException()
    {
        Assert.Throws<ArgumentNullException>(() => new WristbandM2MAuthServiceFactory(null!));
    }
}

using Microsoft.Extensions.Options;

using Moq;

namespace Wristband.AspNet.Auth.M2M.Tests;

public class NamedWristbandM2MAuthServiceTests
{
    private WristbandM2MAuthOptions CreateValidOptions()
    {
        return new WristbandM2MAuthOptions
        {
            WristbandApplicationDomain = "test.wristband.com",
            ClientId = "test-client-id",
            ClientSecret = "test-client-secret",
            BackgroundTokenRefreshInterval = null
        };
    }

    [Fact]
    public void Constructor_SetsNameProperly()
    {
        var expectedName = "clientA";
        var optionsMock = new Mock<IOptions<WristbandM2MAuthOptions>>();
        optionsMock.Setup(o => o.Value).Returns(CreateValidOptions());

        var service = new NamedWristbandM2MAuthService(expectedName, optionsMock.Object);

        Assert.Equal(expectedName, service.Name);
    }

    [Fact]
    public void Constructor_AllowsNullHttpClientFactory()
    {
        var options = Options.Create(CreateValidOptions());

        var service = new NamedWristbandM2MAuthService("clientX", options, null);

        Assert.NotNull(service);
    }

    [Fact]
    public void DifferentInstances_HaveDifferentNames()
    {
        var options = Options.Create(CreateValidOptions());

        var serviceA = new NamedWristbandM2MAuthService("alpha", options);
        var serviceB = new NamedWristbandM2MAuthService("beta", options);

        Assert.NotEqual(serviceA.Name, serviceB.Name);
    }
}

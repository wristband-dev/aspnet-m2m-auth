using Wristband.AspNet.Auth.M2M;

namespace Wristband.Tests.Auth.M2M.Tests;

public class WristbandM2MAuthOptionsTests
{
    [Fact]
    public void Default_TokenExpiryBuffer_Should_Be_60_Seconds()
    {
        var options = new WristbandM2MAuthOptions();
        Assert.Equal(TimeSpan.FromSeconds(60), options.TokenExpiryBuffer);
    }

    [Fact]
    public void Can_Set_And_Get_WristbandApplicationDomain()
    {
        var options = new WristbandM2MAuthOptions
        {
            WristbandApplicationDomain = "example.wristband.io"
        };
        Assert.Equal("example.wristband.io", options.WristbandApplicationDomain);
    }

    [Fact]
    public void Can_Set_And_Get_ClientId()
    {
        var options = new WristbandM2MAuthOptions
        {
            ClientId = "test-client-id"
        };
        Assert.Equal("test-client-id", options.ClientId);
    }

    [Fact]
    public void Can_Set_And_Get_ClientSecret()
    {
        var options = new WristbandM2MAuthOptions
        {
            ClientSecret = "super-secret"
        };
        Assert.Equal("super-secret", options.ClientSecret);
    }

    [Fact]
    public void Can_Set_And_Get_BackgroundTokenRefreshInterval()
    {
        var interval = TimeSpan.FromMinutes(5);
        var options = new WristbandM2MAuthOptions
        {
            BackgroundTokenRefreshInterval = interval
        };
        Assert.Equal(interval, options.BackgroundTokenRefreshInterval);
    }

    [Fact]
    public void TokenExpiryBuffer_Can_Be_Overridden()
    {
        var buffer = TimeSpan.FromSeconds(120);
        var options = new WristbandM2MAuthOptions
        {
            TokenExpiryBuffer = buffer
        };
        Assert.Equal(buffer, options.TokenExpiryBuffer);
    }

    [Fact]
    public void TokenExpiryBuffer_Can_Be_Set_To_Zero()
    {
        var options = new WristbandM2MAuthOptions
        {
            TokenExpiryBuffer = TimeSpan.Zero
        };
        Assert.Equal(TimeSpan.Zero, options.TokenExpiryBuffer);
    }
}

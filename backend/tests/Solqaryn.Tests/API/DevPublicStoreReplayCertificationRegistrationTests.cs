using Microsoft.Extensions.Configuration;
using Solqaryn.API.Middleware;
using Xunit;

namespace Solqaryn.Tests.API;

public sealed class DevPublicStoreReplayCertificationRegistrationTests
{
    [Theory]
    [InlineData(false, false, false, false)]
    [InlineData(false, true, true, false)]
    [InlineData(true, false, true, false)]
    [InlineData(true, true, false, false)]
    [InlineData(true, true, true, true)]
    public void RegistrationRequiresRenderDevelopmentAndExplicitFlag(
        bool isRender, bool isDevelopment, bool enabled, bool expected)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PublicStore:ReplayLiveCertificationOnStartup"] =
                    enabled ? "true" : "false"
            }).Build();

        Assert.Equal(expected,
            DevPublicStoreReplayCertificationHostedService.CanRegister(
                isRender, isDevelopment, configuration));
    }

    [Fact]
    public void MissingFlagRemainsDisabled()
    {
        var configuration = new ConfigurationBuilder().Build();
        Assert.False(DevPublicStoreReplayCertificationHostedService.CanRegister(
            isRender: true, isDevelopment: true, configuration));
    }
}

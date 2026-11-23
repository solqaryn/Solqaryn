using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Moq;
using Solqaryn.API.Security;
using Solqaryn.Application.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.API;

public sealed class C0303M0303DevelopmentRegistrationTests
{
    [Theory]
    [InlineData("Production", false)]
    [InlineData("Staging", false)]
    [InlineData("Development", true)]
    public void AuditProbeRegistrationIsDevOnly(string environmentName, bool expected)
    {
        var environment = new Mock<IHostEnvironment>();
        environment.SetupGet(x => x.EnvironmentName).Returns(environmentName);
        var services = new ServiceCollection();
        services.AddC0303VerifiedAuditDevelopmentOnly(environment.Object);

        Type[] scope =
        [
            typeof(C0303VerifiedAuditTenantReaders),
            typeof(IC0303VerifiedAuditEventReader),
            typeof(IC0303VerifiedAuditResourceReader),
            typeof(C0303SecurityAuditApplicationService)
        ];
        foreach (var type in scope)
        {
            var entries = services.Where(s => s.ServiceType == type).ToArray();
            Assert.Equal(expected ? 1 : 0, entries.Length);
            if (expected)
                Assert.Equal(ServiceLifetime.Scoped, entries[0].Lifetime);
        }
        if (!expected)
            Assert.Empty(services);
    }

    [Theory]
    [InlineData("Development", true, true)]
    [InlineData("Development", false, false)]
    [InlineData("Production", true, false)]
    [InlineData("Staging", true, false)]
    public void ReadOnlyEndpointCannotMapOutsideExplicitDevelopmentOptIn(
        string environmentName, bool enabled, bool expected)
    {
        var environment = new Mock<IHostEnvironment>();
        environment.SetupGet(x => x.EnvironmentName).Returns(environmentName);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SecurityAudit:EnableReadOnlyAuthorizationProbe"] =
                    enabled ? "true" : "false"
            }).Build();
        Assert.Equal(expected,
            C0303AuditReadOnlyProbeEndpoint.ShouldMap(
                environment.Object, configuration));
    }

    [Fact]
    public void NullEnvironmentFailsClosedWithoutPartialRegistration()
    {
        var services = new ServiceCollection();
        Assert.Throws<ArgumentNullException>(() =>
            services.AddC0303VerifiedAuditDevelopmentOnly(null!));
        Assert.Empty(services);
    }
}

using Microsoft.Extensions.DependencyInjection;
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

    [Fact]
    public void NullEnvironmentFailsClosedWithoutPartialRegistration()
    {
        var services = new ServiceCollection();
        Assert.Throws<ArgumentNullException>(() =>
            services.AddC0303VerifiedAuditDevelopmentOnly(null!));
        Assert.Empty(services);
    }
}

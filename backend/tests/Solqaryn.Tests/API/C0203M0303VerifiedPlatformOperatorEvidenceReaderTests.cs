using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Solqaryn.API.Security;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.AutomationV5;
using Solqaryn.Infrastructure.Persistence;
using Xunit;

namespace Solqaryn.Tests.API;

public sealed class C0203M0303VerifiedPlatformOperatorEvidenceReaderTests
{
    private sealed class Current : ICurrentUserService
    {
        public int? UsuarioId => 77;
        public string? NombreUsuario => "user77";
        public string? NombreCompleto => "Server authenticated";
        public int? RolId => 1;
        public bool EsAdministrador => true;
        public bool EstaAutenticado => true;
    }

    private sealed class Platform : IPlataformaScopeService
    {
        public int Reads { get; private set; }
        public Task<UsuarioPlataformaScopeActual?> ObtenerActualAsync(
            CancellationToken ct = default)
        {
            Reads++;
            return Task.FromResult<UsuarioPlataformaScopeActual?>(null);
        }
    }

    private static (C0203VerifiedPlatformOperatorEvidenceReader Reader,
        Platform Platform) Build(bool enabled, bool allowSuspend,
            bool authenticated = true, string? tenantRoute = "42",
            int policyRevision = 1)
    {
        var http = new DefaultHttpContext();
        if (authenticated)
            http.User = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim("id", "77"), new Claim("nombreUsuario", "user77") },
                authenticationType: "synthetic-must-not-grant"));
        if (tenantRoute is not null)
            http.Request.RouteValues["tenantId"] = tenantRoute;

        var values = new Dictionary<string, string?>
        {
            ["TenantLifecycle:EnableReadOnlyAuthorizationProbe"] = enabled.ToString(),
            ["TenantLifecycle:AllowSuspendReadOnly"] = allowSuspend.ToString(),
            ["TenantLifecycle:ReadOnlyPolicyRevision"] =
                policyRevision.ToString(System.Globalization.CultureInfo.InvariantCulture)
        };
        var settings = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("c0203-guard-" + Guid.NewGuid()).Options;
        var db = new AppDbContext(dbOptions);
        var platform = new Platform();
        return (new C0203VerifiedPlatformOperatorEvidenceReader(
            new HttpContextAccessor { HttpContext = http },
            new Current(), platform, db, settings), platform);
    }

    [Theory]
    [InlineData(false, true, 1)]
    [InlineData(true, false, 1)]
    [InlineData(true, true, 0)]
    public async Task DefaultOffMissingCommandOrPolicyRevisionNeverReadsGrants(
        bool enabled, bool allowSuspend, int revision)
    {
        var (reader, platform) = Build(enabled, allowSuspend, policyRevision: revision);
        Assert.Null(await reader.ReadAsync("42",
            V5TenantLifecycleDomainRules.Command.Suspend, default));
        Assert.Equal(0, platform.Reads);
    }

    [Theory]
    [InlineData(V5TenantLifecycleDomainRules.Command.Reactivate)]
    [InlineData(V5TenantLifecycleDomainRules.Command.Archive)]
    [InlineData(V5TenantLifecycleDomainRules.Command.LogicalDelete)]
    public async Task DestructiveOrReactivationCommandsRemainExplicitlyDenied(
        V5TenantLifecycleDomainRules.Command command)
    {
        var (reader, platform) = Build(true, true);
        Assert.Null(await reader.ReadAsync("42", command, default));
        Assert.Equal(0, platform.Reads);
    }

    [Theory]
    [InlineData("42", "99")]
    [InlineData("42", null)]
    [InlineData("company-A", "42")]
    [InlineData("0", "0")]
    public async Task ForeignOrMissingRouteAndInvalidTenantCannotEscalate(
        string tenant, string? route)
    {
        var (reader, platform) = Build(true, true, tenantRoute: route);
        Assert.Null(await reader.ReadAsync(tenant,
            V5TenantLifecycleDomainRules.Command.Suspend, default));
        Assert.Equal(0, platform.Reads);
    }

    [Fact]
    public async Task NotAuthenticatedUserCannotClaimGlobalRole()
    {
        var (reader, platform) = Build(true, true, authenticated: false);
        Assert.Null(await reader.ReadAsync("42",
            V5TenantLifecycleDomainRules.Command.Suspend, default));
        Assert.Equal(0, platform.Reads);
    }

    [Fact]
    public async Task SyntheticPrincipalWithoutRealBearerHandlerNeverProvesGrant()
    {
        var (reader, platform) = Build(true, true);
        // Even a seemingly authenticated ClaimsPrincipal is not a verified JWT.
        Assert.Null(await reader.ReadAsync("42",
            V5TenantLifecycleDomainRules.Command.Suspend, default));
        Assert.Equal(0, platform.Reads);
    }

    [Fact]
    public async Task CancellationBeforeLookupIsNotTurnedIntoAFalseDenial()
    {
        var (reader, platform) = Build(true, true);
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await reader.ReadAsync("42",
                V5TenantLifecycleDomainRules.Command.Suspend, cts.Token));
        Assert.Equal(0, platform.Reads);
    }
}

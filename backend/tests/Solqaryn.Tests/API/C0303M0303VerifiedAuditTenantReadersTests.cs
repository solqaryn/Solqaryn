using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Solqaryn.API.Security;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Rules = Solqaryn.Domain.Security.C0303SecurityAuditDomainRules;
using Xunit;

namespace Solqaryn.Tests.API;

public sealed class C0303M0303VerifiedAuditTenantReadersTests
{
    private const int Tenant = 202, Actor = 91, Role = 71;
    private static readonly SymmetricSecurityKey Signing =
        new(Encoding.UTF8.GetBytes("solqaryn-c0303-m0303-audit-jwt-test-key-2026!"));

    private static string Token(int actor = Actor, bool valid = true)
    {
        var key = valid ? Signing : new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes("solqaryn-c0303-invalid-signed-jwt-key-2026!"));
        var jwt = new JwtSecurityToken(
            issuer: "c0303-tests", audience: "c0303-tests",
            claims: [new Claim("id", actor.ToString()),
                     new Claim("nombreUsuario", "auditor91"),
                     new Claim("sub", "auditor91")],
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddMinutes(8),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    private sealed record Fixture(
        C0303VerifiedAuditTenantReaders Reader,
        AppDbContext Database, ServiceProvider Services,
        DefaultHttpContext Http, Mock<IUsuarioScopeService> Scopes)
        : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            await Database.DisposeAsync();
            await Services.DisposeAsync();
        }
    }

    private static async Task<Fixture> BuildAsync(
        bool enabled = true, bool marker = true, bool grant = true,
        bool admin = true, int membershipTenant = Tenant, int role = Role,
        string route = "202", string? token = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true, ValidIssuer = "c0303-tests",
                    ValidateAudience = true, ValidAudience = "c0303-tests",
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = Signing,
                    ValidateLifetime = true, ClockSkew = TimeSpan.Zero
                };
            });
        var serviceProvider = services.BuildServiceProvider();
        var http = new DefaultHttpContext { RequestServices = serviceProvider };
        http.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("id", Actor.ToString())], "synthetic-cannot-authorize"));
        http.Request.Headers.Authorization = "Bearer " + (token ?? Token());
        http.Request.RouteValues["empresaId"] = route;
        if (marker)
            http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = Tenant;

        var current = new Mock<ICurrentUserService>();
        current.SetupGet(c => c.EstaAutenticado).Returns(true);
        current.SetupGet(c => c.UsuarioId).Returns(Actor);
        current.SetupGet(c => c.NombreUsuario).Returns("auditor91");
        var scopes = new Mock<IUsuarioScopeService>();
        scopes.Setup(s => s.ObtenerActualAsync(It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((int requested, CancellationToken ct) =>
                new UsuarioTenantScopeActual(Actor, membershipTenant, role,
                    "Auditor", true));
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("c0303-security-" + Guid.NewGuid()).Options;
        var db = new AppDbContext(options);
        db.Set<Empresa>().Add(new Empresa("Tenant A") { Id = Tenant });
        db.Roles.Add(new Rol
        {
            Id = Role, Nombre = "Auditor", EmpresaId = Tenant,
            Ambito = AmbitoAutorizacion.Empresa, EsAdministrador = admin, Activo = true
        });
        db.Usuarios.Add(new Usuario
        {
            Id = Actor, NombreUsuario = "auditor91", NombreCompleto = "Audit",
            PasswordHash = "test-only", RolId = Role, Activo = true
        });
        db.UsuarioEmpresas.Add(new UsuarioEmpresa(Actor, Tenant, Role));
        if (grant)
        {
            db.Permisos.Add(new Permiso
            {
                Id = 12, Codigo = "AUDITORIA.VER", Nombre = "Auditoria.Ver",
                Modulo = ModuloSistema.Auditoria, Accion = AccionPermiso.Ver,
                Ambito = AmbitoAutorizacion.Empresa, Activo = true
            });
            db.RolPermisos.Add(new RolPermiso { Id = 19, RolId = Role, PermisoId = 12 });
        }
        await db.SaveChangesAsync();

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["SecurityAudit:EnableReadOnlyAuthorizationProbe"] = enabled.ToString()
            }).Build();
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.SetupGet(x => x.HttpContext).Returns(http);
        var reader = new C0303VerifiedAuditTenantReaders(
            accessor.Object, current.Object, scopes.Object, db, configuration);
        return new Fixture(reader, db, serviceProvider, http, scopes);
    }

    [Fact]
    public async Task ActualSignedJwtTenantAdminAndRelationalGrantReturnsNonAuthorizingCandidate()
    {
        await using var f = await BuildAsync();
        var actor = await f.Reader.ReadCurrentAsync(default);
        Assert.NotNull(actor);
        Assert.Equal(Tenant, actor.TenantId);
        Assert.Equal(Actor, actor.ActorId);
        Assert.True(actor.SignedJwtVerified);
        Assert.Equal(Rules.Scope.Enterprise, actor.Scope);
        Assert.Equal(Rules.Action.AuditRead, actor.Action);
        Assert.False(actor.TransactionalSinkVerified);
        var resource = await f.Reader.ReadCurrentAsync(
            Actor, Tenant, Rules.Action.AuditRead, default);
        Assert.NotNull(resource);
        Assert.Equal(Tenant, resource.PersistedTenantId);
    }

    [Fact]
    public async Task DefaultDisabledAndMissingServerVerifiedTenantAreBothFailClosed()
    {
        await using var disabled = await BuildAsync(enabled: false);
        Assert.Null(await disabled.Reader.ReadCurrentAsync(default));
        await using var unverified = await BuildAsync(marker: false);
        Assert.Null(await unverified.Reader.ReadCurrentAsync(default));
    }

    [Theory]
    [InlineData("0202")]
    [InlineData("999")]
    [InlineData("202;DROP TABLE")]
    public async Task TenantRouteCannotOverrideVerifiedServerTenant(string route)
    {
        await using var f = await BuildAsync(route: route);
        Assert.Null(await f.Reader.ReadCurrentAsync(default));
    }

    [Fact]
    public async Task ForgedJwtAndSignedActorMismatchDeniedDespiteSyntheticPrincipal()
    {
        await using var bad = await BuildAsync(token: Token(valid: false));
        Assert.Null(await bad.Reader.ReadCurrentAsync(default));
        await using var other = await BuildAsync(token: Token(actor: 92));
        Assert.Null(await other.Reader.ReadCurrentAsync(default));
    }

    [Fact]
    public async Task CrossTenantMembershipNonAdminAndMissingGrantDeny()
    {
        await using var cross = await BuildAsync(membershipTenant: 303);
        Assert.Null(await cross.Reader.ReadCurrentAsync(default));
        await using var nonadmin = await BuildAsync(admin: false);
        Assert.Null(await nonadmin.Reader.ReadCurrentAsync(default));
        await using var denied = await BuildAsync(grant: false);
        Assert.Null(await denied.Reader.ReadCurrentAsync(default));
    }

    [Fact]
    public async Task ActualRelationalRevocationAndUserBlockDenySubsequentReads()
    {
        await using var f = await BuildAsync();
        Assert.NotNull(await f.Reader.ReadCurrentAsync(default));
        f.Database.RolPermisos.Remove(await f.Database.RolPermisos.SingleAsync());
        await f.Database.SaveChangesAsync();
        Assert.Null(await f.Reader.ReadCurrentAsync(default));

        await using var blocked = await BuildAsync();
        var user = await blocked.Database.Usuarios.SingleAsync();
        user.Bloqueado = true;
        await blocked.Database.SaveChangesAsync();
        Assert.Null(await blocked.Reader.ReadCurrentAsync(default));
    }

    [Fact]
    public async Task ArbitraryBusinessActionsAndCrossTenantResourcesNeverAttestOwnership()
    {
        await using var f = await BuildAsync();
        Assert.Null(await f.Reader.ReadCurrentAsync(
            Actor, Tenant, Rules.Action.CriticalChange, default));
        Assert.Null(await f.Reader.ReadCurrentAsync(
            Actor, 303, Rules.Action.AuditRead, default));
        Assert.Null(await f.Reader.ReadCurrentAsync(
            Actor + 1, Tenant, Rules.Action.AuditRead, default));
    }

    [Fact]
    public async Task CancellationIsPropagatedWithoutFalseSuccess()
    {
        await using var f = await BuildAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await f.Reader.ReadCurrentAsync(new CancellationToken(true)));
    }
}

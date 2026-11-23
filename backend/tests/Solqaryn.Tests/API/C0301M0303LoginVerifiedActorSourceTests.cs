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
using Solqaryn.Application.AutomationV5;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Xunit;

namespace Solqaryn.Tests.API;

public sealed class C0301M0303LoginVerifiedActorSourceTests
{
    private const int Tenant = 11;
    private const int Actor = 91;
    private const int Role = 23;
    private const int Branch = 111;
    private static readonly SymmetricSecurityKey Key =
        new(Encoding.UTF8.GetBytes("solqaryn-login-m0303-only-test-key-2026!"));

    private static string Token(int actor = Actor, bool validSignature = true)
    {
        var key = validSignature ? Key :
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                "invalid-login-jwt-test-signature-key-2026!"));
        var jwt = new JwtSecurityToken(
            issuer: "solqaryn-login-tests", audience: "solqaryn-login-tests",
            claims: [new Claim("id", actor.ToString())],
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    private static async Task<Fixture> SetupAsync(
        bool enabled = true, string tenantRoute = "11", string branchRoute = "111",
        bool grant = true, int scopeTenant = Tenant, int scopeRole = Role,
        int? roleTenant = Tenant, int branchTenant = Tenant,
        string? token = null, bool branchActive = true,
        bool roleIsSystem = false)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true, ValidIssuer = "solqaryn-login-tests",
                    ValidateAudience = true, ValidAudience = "solqaryn-login-tests",
                    ValidateIssuerSigningKey = true, IssuerSigningKey = Key,
                    ValidateLifetime = true, ClockSkew = TimeSpan.Zero
                };
            });
        var provider = services.BuildServiceProvider();
        var http = new DefaultHttpContext { RequestServices = provider };
        http.Request.RouteValues["tenantId"] = tenantRoute;
        http.Request.RouteValues["branchId"] = branchRoute;
        http.Request.Headers.Authorization = "Bearer " + (token ?? Token());
        // This forged principal alone can NEVER authenticate the source.
        http.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("id", Actor.ToString())], "fake-test-principal"));

        var current = new Mock<ICurrentUserService>();
        current.SetupGet(x => x.EstaAutenticado).Returns(true);
        current.SetupGet(x => x.UsuarioId).Returns(Actor);
        var membership = new Mock<IUsuarioScopeService>();
        membership.Setup(x => x.ObtenerActualAsync(
            It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int tenant, CancellationToken _) =>
                new UsuarioTenantScopeActual(Actor, scopeTenant, scopeRole, "LoginReader", false));
        var database = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        database.Roles.Add(new Rol
        {
            Id = Role, Nombre = "LoginReader", EmpresaId = roleTenant,
            EsSistema = roleIsSystem,
            Ambito = AmbitoAutorizacion.Empresa, Activo = true
        });
        database.Set<Sucursal>().Add(new Sucursal
        {
            Id = Branch, EmpresaId = branchTenant, Codigo = "GYM-01",
            Nombre = "Sucursal gimnasio", Activa = branchActive
        });
        if (grant)
        {
            database.Permisos.Add(new Permiso
            {
                Id = 7, Codigo = "SUCURSALES.VER", Nombre = "Sucursales.Ver",
                Modulo = ModuloSistema.Sucursales, Accion = AccionPermiso.Ver,
                Ambito = AmbitoAutorizacion.Empresa, Activo = true
            });
            database.RolPermisos.Add(
                new RolPermiso { Id = 7, RolId = Role, PermisoId = 7 });
        }
        await database.SaveChangesAsync();
        var settings = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>
            { ["LoginSecurity:EnableReadOnlyProbe"] = enabled.ToString() }).Build();
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.SetupGet(x => x.HttpContext).Returns(http);
        var reader = new C0301LoginVerifiedActorSource(
            accessor.Object, current.Object, membership.Object, database, settings);
        return new Fixture(reader, database, provider);
    }

    private sealed record Fixture(
        C0301LoginVerifiedActorSource Reader, AppDbContext Database,
        ServiceProvider Provider) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            await Database.DisposeAsync();
            await Provider.DisposeAsync();
        }
    }

    [Fact]
    public async Task SignedJwtCurrentScopeBranchAndGrantAreRealButNeverLoginAuthorization()
    {
        await using var f = await SetupAsync();
        var result = await f.Reader.ReadCurrentAsync(CancellationToken.None);
        Assert.NotNull(result);
        Assert.Equal(Tenant, result!.TenantId);
        Assert.Equal(Actor, result.ActorId);
        Assert.Equal(Branch, result.BranchId);
        Assert.True(result.SignedJwtValidated);
        Assert.True(result.CurrentMembershipAndRbacFromDatabase);
        Assert.False(result.GrantsTenantAccess);
        Assert.False(result.IssuesJwt);
        Assert.False(result.GrantsPlatformAccess);
    }

    [Fact]
    public async Task DisabledByDefaultRejectsValidTokenAndCurrentMembership()
    {
        await using var f = await SetupAsync(enabled: false);
        Assert.Null(await f.Reader.ReadCurrentAsync(CancellationToken.None));
    }

    [Theory]
    [InlineData("0", "111")]
    [InlineData("011", "111")]
    [InlineData("12", "111")]
    [InlineData("11", "0")]
    [InlineData("11", "0111")]
    [InlineData("11", "112")]
    [InlineData("11;DROP", "111")]
    public async Task NonCanonicalOrCrossTenantAndBranchRoutesDeny(
        string tenant, string branch)
    {
        await using var f = await SetupAsync(tenantRoute: tenant, branchRoute: branch);
        Assert.Null(await f.Reader.ReadCurrentAsync(CancellationToken.None));
    }

    [Fact]
    public async Task WrongSignatureOrActorNeverTrustsForgedHttpPrincipal()
    {
        await using var forged = await SetupAsync(token: Token(validSignature: false));
        Assert.Null(await forged.Reader.ReadCurrentAsync(CancellationToken.None));
        await using var wrongId = await SetupAsync(token: Token(actor: 92));
        Assert.Null(await wrongId.Reader.ReadCurrentAsync(CancellationToken.None));
    }

    [Fact]
    public async Task TenantRoleAndPhysicalBranchAreScopedToTenantA()
    {
        await using var foreignMember = await SetupAsync(scopeTenant: 22);
        Assert.Null(await foreignMember.Reader.ReadCurrentAsync(CancellationToken.None));
        await using var foreignRole = await SetupAsync(roleTenant: 22);
        Assert.Null(await foreignRole.Reader.ReadCurrentAsync(CancellationToken.None));
        await using var foreignBranch = await SetupAsync(branchTenant: 22);
        Assert.Null(await foreignBranch.Reader.ReadCurrentAsync(CancellationToken.None));
        await using var wrongRole = await SetupAsync(scopeRole: 99);
        Assert.Null(await wrongRole.Reader.ReadCurrentAsync(CancellationToken.None));
        await using var closedBranch = await SetupAsync(branchActive: false);
        Assert.Null(await closedBranch.Reader.ReadCurrentAsync(CancellationToken.None));
    }

    [Fact]
    public async Task CurrentTenantMayUseActiveSharedEnterpriseRoleTemplateWithoutGrantingAccess()
    {
        // Core UsuarioScopeService accepts EmpresaId=null ONLY for a system
        // enterprise template. The DEV JWT/branch probe must agree.
        await using var f = await SetupAsync(roleTenant: null, roleIsSystem: true);
        var result = await f.Reader.ReadCurrentAsync(CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(Role, result!.RoleId);
        Assert.Equal(Tenant, result.TenantId);
        Assert.True(result.SignedJwtValidated);
        Assert.True(result.CurrentMembershipAndRbacFromDatabase);
        Assert.False(result.IssuesJwt);
        Assert.False(result.GrantsTenantAccess);
        Assert.False(result.GrantsPlatformAccess);
    }

    [Fact]
    public async Task UnownedNonTemplateRoleCannotPersistAnEnterpriseGrant()
    {
        // The real persistence ownership guard rejects this invalid fixture
        // BEFORE the read-only JWT actor source can ever run. Assert that
        // security boundary instead of treating its rejection as test setup.
        await Assert.ThrowsAsync<BusinessRuleException>(async () =>
        {
            await using var f = await SetupAsync(roleTenant: null, roleIsSystem: false);
        });
    }

    [Fact]
    public async Task ForeignOwnedSystemRoleDoesNotCrossTenantDespiteTemplateFlag()
    {
        await using var f = await SetupAsync(roleTenant: 22, roleIsSystem: true);
        Assert.Null(await f.Reader.ReadCurrentAsync(CancellationToken.None));
    }

    [Fact]
    public async Task RevokedRelationalGrantFailsClosedOnNextRequest()
    {
        await using var f = await SetupAsync();
        Assert.NotNull(await f.Reader.ReadCurrentAsync(CancellationToken.None));
        f.Database.RolPermisos.Remove(
            await f.Database.RolPermisos.SingleAsync());
        await f.Database.SaveChangesAsync();
        Assert.Null(await f.Reader.ReadCurrentAsync(CancellationToken.None));
    }

    [Fact]
    public async Task NoBranchPermissionIsNeverTreatedAsLoginGrant()
    {
        await using var f = await SetupAsync(grant: false);
        Assert.Null(await f.Reader.ReadCurrentAsync(CancellationToken.None));
    }

    [Fact]
    public async Task CancellationIsPreservedNotMistakenForAuthorizationDenial()
    {
        await using var f = await SetupAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await f.Reader.ReadCurrentAsync(new CancellationToken(true)));
    }
}

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
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using System.IdentityModel.Tokens.Jwt;
using Xunit;

namespace Solqaryn.Tests.API;

public sealed class C0402M0303ContractVerifiedActorTests
{
    private const int Tenant = 100;
    private const int Actor = 17;
    private const int Role = 23;
    private static readonly SymmetricSecurityKey SigningKey =
        new(Encoding.UTF8.GetBytes("solqaryn-v22-c0402-m0303-test-key-2026!"));

    private static string Token(int actor = Actor, bool validSignature = true)
    {
        var key = validSignature ? SigningKey :
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                "another-invalid-signing-key-for-tests!"));
        var jwt = new JwtSecurityToken(
            issuer: "solqaryn-v22-tests", audience: "solqaryn-v22-tests",
            claims: [new Claim("id", actor.ToString())],
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    private static async Task<Fixture> SetupAsync(bool enabled = true,
        bool billingGrant = true, bool configGrant = true,
        string route = "100", string? token = null,
        int membershipTenant = Tenant, int membershipRole = Role)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true, ValidIssuer = "solqaryn-v22-tests",
                    ValidateAudience = true, ValidAudience = "solqaryn-v22-tests",
                    ValidateIssuerSigningKey = true, IssuerSigningKey = SigningKey,
                    ValidateLifetime = true, ClockSkew = TimeSpan.Zero
                };
            });
        var provider = services.BuildServiceProvider();
        var http = new DefaultHttpContext { RequestServices = provider };
        http.Request.RouteValues["empresaId"] = route;
        http.Request.Headers.Authorization = "Bearer " + (token ?? Token());
        // A synthetic principal alone must NEVER pass: real JWT bearer is checked.
        http.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("id", Actor.ToString())], "fake-existing-identity"));

        var current = new Mock<ICurrentUserService>();
        current.SetupGet(x => x.EstaAutenticado).Returns(true);
        current.SetupGet(x => x.UsuarioId).Returns(Actor);
        var memberships = new Mock<IUsuarioScopeService>();
        memberships.Setup(x => x.ObtenerActualAsync(
            It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int requestedTenant, CancellationToken requestedCancellation) =>
                new UsuarioTenantScopeActual(Actor, membershipTenant, membershipRole,
                    "ReadOnlyContractReviewer", false));

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var database = new AppDbContext(options);
        database.Roles.Add(new Rol
        {
            Id = Role, Nombre = "ReadOnlyContractReviewer",
            EmpresaId = Tenant, Ambito = AmbitoAutorizacion.Empresa,
            Activo = true
        });
        if (billingGrant)
        {
            database.Permisos.Add(new Permiso
            {
                Id = 1, Codigo = "FACTURACION.VER", Nombre = "Facturacion.Ver",
                Modulo = ModuloSistema.Facturacion, Accion = AccionPermiso.Ver,
                Ambito = AmbitoAutorizacion.Empresa, Activo = true
            });
            database.RolPermisos.Add(new RolPermiso { Id = 1, RolId = Role, PermisoId = 1 });
        }
        if (configGrant)
        {
            database.Permisos.Add(new Permiso
            {
                Id = 2, Codigo = "CONFIGURACION.VER", Nombre = "Configuracion.Ver",
                Modulo = ModuloSistema.Configuracion, Accion = AccionPermiso.Ver,
                Ambito = AmbitoAutorizacion.Empresa, Activo = true
            });
            database.RolPermisos.Add(new RolPermiso { Id = 2, RolId = Role, PermisoId = 2 });
        }
        await database.SaveChangesAsync();
        var config = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["ContractAuthorization:EnableReadOnlyActorProbe"] = enabled.ToString()
            }).Build();
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.SetupGet(x => x.HttpContext).Returns(http);
        var source = new C0402ContractVerifiedActorSource(
            accessor.Object,
            current.Object, memberships.Object, database, config);
        return new Fixture(source, database, provider, http);
    }

    private sealed record Fixture(C0402ContractVerifiedActorSource Source,
        AppDbContext Database, ServiceProvider Provider, DefaultHttpContext Http)
        : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            await Database.DisposeAsync();
            await Provider.DisposeAsync();
        }
    }

    [Fact]
    public async Task SignedJwtAndBothCurrentRelationalGrantsReturnReadOnlySnapshot()
    {
        await using var f = await SetupAsync();
        var result = await f.Source.ReadCurrentAsync(CancellationToken.None);
        Assert.NotNull(result);
        Assert.Equal(Tenant, result.Scope.EmpresaId);
        Assert.Equal(Actor, result.Scope.ActorId);
        Assert.True(result.SignedJwtEstablished);
        Assert.True(result.RelationalRbacFresh);
    }

    [Fact]
    public async Task DisabledByDefaultDeniesEvenWithValidSignedJwt()
    {
        await using var f = await SetupAsync(enabled: false);
        Assert.Null(await f.Source.ReadCurrentAsync(CancellationToken.None));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("0100")]
    [InlineData("200")]
    [InlineData("100;DROP TABLE")]
    public async Task TenantRouteMustBeCanonicalAndMemberOwned(string tenant)
    {
        await using var f = await SetupAsync(route: tenant);
        Assert.Null(await f.Source.ReadCurrentAsync(CancellationToken.None));
    }

    [Fact]
    public async Task ForgedSignedTokenOrWrongActorDeniedDespitePrincipal()
    {
        await using var badSignature = await SetupAsync(token: Token(validSignature: false));
        Assert.Null(await badSignature.Source.ReadCurrentAsync(CancellationToken.None));
        await using var wrongActor = await SetupAsync(token: Token(actor: 18));
        Assert.Null(await wrongActor.Source.ReadCurrentAsync(CancellationToken.None));
    }

    [Fact]
    public async Task MissingGrantOrWrongTenantRoleAlwaysDenies()
    {
        await using var noBilling = await SetupAsync(billingGrant: false);
        Assert.Null(await noBilling.Source.ReadCurrentAsync(CancellationToken.None));
        await using var noConfig = await SetupAsync(configGrant: false);
        Assert.Null(await noConfig.Source.ReadCurrentAsync(CancellationToken.None));
        await using var wrongTenant = await SetupAsync(membershipTenant: 200);
        Assert.Null(await wrongTenant.Source.ReadCurrentAsync(CancellationToken.None));
        await using var wrongRole = await SetupAsync(membershipRole: 24);
        Assert.Null(await wrongRole.Source.ReadCurrentAsync(CancellationToken.None));
    }

    [Fact]
    public async Task RevocationFromRelationalPermissionsDeniesNextRead()
    {
        await using var f = await SetupAsync();
        Assert.NotNull(await f.Source.ReadCurrentAsync(CancellationToken.None));
        var grant = await f.Database.RolPermisos.SingleAsync(x => x.PermisoId == 2);
        f.Database.RolPermisos.Remove(grant);
        await f.Database.SaveChangesAsync();
        Assert.Null(await f.Source.ReadCurrentAsync(CancellationToken.None));
    }

    [Fact]
    public async Task CanceledRequestIsNotEligibleForSnapshot()
    {
        await using var f = await SetupAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await f.Source.ReadCurrentAsync(new CancellationToken(true)));
    }
}

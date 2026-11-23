using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using MySql.Data.MySqlClient;
using MySql.EntityFrameworkCore.Extensions;
using Solqaryn.API.Security;
using Solqaryn.Application.AutomationV5;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Services;
using Xunit;

namespace Solqaryn.Tests.Integration;

/// <summary>
/// C08-03.M03.03: actual DEV-only read source, signed HTTP bearer and Oracle EF
/// on an isolated MySQL 8.4 database. Never creates a production route or a
/// gym payment, membership, door or hardware authorization.
/// </summary>
[Trait("Category", "Integration")]
public sealed class C0803M0303RealMysql84HttpIntegrationTests
{
    private const string BaseDatabase = "solqaryn_c1502_m0503";
    private const string Issuer = "c0803-m0303-test-only";
    private const string Audience = "c0803-m0303-client-only";
    private static readonly SymmetricSecurityKey Key = new(
        Encoding.UTF8.GetBytes("c0803-gym-real-mysql84-signed-jwt-test-only-2026"));
    private static readonly SymmetricSecurityKey ForgedKey = new(
        Encoding.UTF8.GetBytes("c0803-gym-real-mysql84-forged-jwt-test-only-2026"));

    private static string BaseConnection()
    {
        var raw = Environment.GetEnvironmentVariable("C1502_M0503_MYSQL_TEST")
            ?? throw new InvalidOperationException("EPHEMERAL_MYSQL84_REQUIRED");
        var parsed = new MySqlConnectionStringBuilder(raw);
        if (parsed.Server != "127.0.0.1" || parsed.Port != 3306 ||
            parsed.Database != BaseDatabase || parsed.UserID != "root" ||
            parsed.SslMode != MySqlSslMode.Disabled)
            throw new InvalidOperationException("EPHEMERAL_MYSQL84_LOOPBACK_ONLY");
        return raw;
    }

    private static string Token(int actor, bool forged = false)
    {
        return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            issuer: Issuer, audience: Audience,
            claims: [new Claim("id", actor.ToString(
                System.Globalization.CultureInfo.InvariantCulture))],
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(
                forged ? ForgedKey : Key, SecurityAlgorithms.HmacSha256)));
    }

    private static async Task<IHost> StartAsync(string connection, bool enableProbe = true)
    {
        var settings = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["GymAuthorization:EnableReadOnlyProbe"] = enableProbe ? "true" : "false"
            }).Build();
        return await new HostBuilder().ConfigureWebHost(web => web.UseTestServer()
            .ConfigureServices(services =>
            {
                services.AddSingleton<IConfiguration>(settings);
                services.AddRouting();
                services.AddHttpContextAccessor();
                services.AddDbContext<AppDbContext>(options => options.UseMySQL(
                    connection, mysql => mysql.MigrationsAssembly(
                        "Solqaryn.Infrastructure.Migrations")));
                services.AddScoped<ICurrentUserService, CurrentUserService>();
                services.AddScoped<IUsuarioScopeService, UsuarioScopeService>();
                services.AddScoped<IC0803VerifiedGymActorSource,
                    C0803GymVerifiedActorSource>();
                services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(options =>
                    {
                        options.MapInboundClaims = false;
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuer = true, ValidIssuer = Issuer,
                            ValidateAudience = true, ValidAudience = Audience,
                            ValidateIssuerSigningKey = true, IssuerSigningKey = Key,
                            ValidateLifetime = true, ClockSkew = TimeSpan.Zero,
                            RequireSignedTokens = true, RequireExpirationTime = true
                        };
                    });
                services.AddAuthorization();
            }).Configure(app =>
            {
                app.UseRouting();
                app.UseAuthentication();
                app.UseAuthorization();
                app.UseEndpoints(endpoints =>
                {
                    if (enableProbe)
                        C0803GymReadOnlyProbeEndpoint.Map(endpoints);
                });
            })).StartAsync();
    }

    private static async Task<HttpResponseMessage> ReadAsync(
        HttpClient client, int tenant, int branch, string? jwt)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get,
            $"/__dev/gym-auth-readonly/{tenant}/{branch}");
        if (jwt is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Bearer", jwt);
        request.Headers.Add("X-Empresa-Id", "999999");
        request.Headers.Add("X-Rol-Id", "999999");
        return await client.SendAsync(request);
    }

    [Fact]
    public async Task DevProbeIsNotMappedWithoutExplicitOptIn()
    {
        using var host = await StartAsync(BaseConnection(), enableProbe: false);
        using var client = host.GetTestClient();
        using var response = await ReadAsync(client, 1, 1, Token(1));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SignedJwtRealMembershipMysqlTenantABGrantRevocationAndRollback()
    {
        var original = BaseConnection();
        var schema = "c0803_m0303_" + Guid.NewGuid().ToString("N");
        var scoped = original.Replace("Database=" + BaseDatabase + ";",
            "Database=" + schema + ";", StringComparison.Ordinal);
        if (scoped == original)
            throw new InvalidOperationException("ISOLATED_SCHEMA_REQUIRED");

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL(scoped, mysql => mysql.MigrationsAssembly(
                "Solqaryn.Infrastructure.Migrations")).Options;
        try
        {
            await using var db = new AppDbContext(options);
            await Phase7MySqlTestDatabase.InitializeFreshAsync(db);
            await db.Database.OpenConnectionAsync();
            await using (var cmd = db.Database.GetDbConnection().CreateCommand())
            {
                cmd.CommandText = "SELECT VERSION()";
                Assert.StartsWith("8.4.",
                    Convert.ToString(await cmd.ExecuteScalarAsync()));
            }
            await db.Database.CloseConnectionAsync();

            var tenantA = new Empresa("C0803 gym isolated A");
            var tenantB = new Empresa("C0803 gym isolated B");
            db.Set<Empresa>().AddRange(tenantA, tenantB);
            await db.SaveChangesAsync();

            var roleA = new Rol
            {
                Nombre = "C0803 gym reader", NombreNormalizado = "C0803_GYM",
                Ambito = AmbitoAutorizacion.Empresa,
                EmpresaId = tenantA.Id, Activo = true
            };
            db.Roles.Add(roleA);
            await db.SaveChangesAsync();
            var actor = new Usuario
            {
                NombreUsuario = "gym_" + Guid.NewGuid().ToString("N")[..12],
                NombreCompleto = "Gym test actor",
                PasswordHash = "never-used-test-only",
                RolId = roleA.Id, Activo = true
            };
            db.Usuarios.Add(actor);
            await db.SaveChangesAsync();
            var membership = new UsuarioEmpresa(actor.Id, tenantA.Id, roleA.Id);
            db.UsuarioEmpresas.Add(membership);
            var branchA = new Sucursal
            {
                EmpresaId = tenantA.Id, Codigo = "GYM-A",
                Nombre = "Gym A", Activa = true
            };
            var branchB = new Sucursal
            {
                EmpresaId = tenantB.Id, Codigo = "GYM-B",
                Nombre = "Gym B", Activa = true
            };
            db.Set<Sucursal>().AddRange(branchA, branchB);
            var permission = await db.Permisos.IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.Modulo == ModuloSistema.Sucursales &&
                    p.Accion == AccionPermiso.Ver &&
                    p.Ambito == AmbitoAutorizacion.Empresa &&
                    p.Activo && !p.Eliminado);
            if (permission is null)
            {
                permission = new Permiso
                {
                    Codigo = "C0803.SUCURSALES.VER",
                    Nombre = "C0803 isolated branch read",
                    Modulo = ModuloSistema.Sucursales,
                    Accion = AccionPermiso.Ver,
                    Ambito = AmbitoAutorizacion.Empresa,
                    Activo = true
                };
                db.Permisos.Add(permission);
            }
            await db.SaveChangesAsync();
            var grant = new RolPermiso { RolId = roleA.Id, PermisoId = permission.Id };
            db.RolPermisos.Add(grant);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            using var host = await StartAsync(scoped);
            using var client = host.GetTestClient();
            var jwt = Token(actor.Id);
            using (var anonymous = await ReadAsync(
                client, tenantA.Id, branchA.Id, null))
                Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
            using (var forged = await ReadAsync(
                client, tenantA.Id, branchA.Id, Token(actor.Id, forged: true)))
                Assert.Equal(HttpStatusCode.Unauthorized, forged.StatusCode);
            using (var wrongActor = await ReadAsync(
                client, tenantA.Id, branchA.Id, Token(actor.Id + 1)))
                Assert.Equal(HttpStatusCode.Forbidden, wrongActor.StatusCode);
            using (var foreignTenant = await ReadAsync(
                client, tenantB.Id, branchB.Id, jwt))
                Assert.Equal(HttpStatusCode.Forbidden, foreignTenant.StatusCode);
            using (var foreignBranch = await ReadAsync(
                client, tenantA.Id, branchB.Id, jwt))
                Assert.Equal(HttpStatusCode.Forbidden, foreignBranch.StatusCode);
            using (var positive = await ReadAsync(
                client, tenantA.Id, branchA.Id, jwt))
            {
                Assert.Equal(HttpStatusCode.OK, positive.StatusCode);
                Assert.Equal("READONLY_NO_GYM_GRANT",
                    await positive.Content.ReadAsStringAsync());
            }

            // A signed token must not preserve a role disabled in current MySQL.
            var liveRole = await db.Roles.SingleAsync(r => r.Id == roleA.Id);
            liveRole.Activo = false;
            await db.SaveChangesAsync();
            using (var disabledRole = await ReadAsync(
                client, tenantA.Id, branchA.Id, jwt))
                Assert.Equal(HttpStatusCode.Forbidden, disabledRole.StatusCode);
            liveRole.Activo = true;
            await db.SaveChangesAsync();

            var committedGrant = await db.RolPermisos
                .SingleAsync(g => g.RolId == roleA.Id &&
                    g.PermisoId == permission.Id);
            db.RolPermisos.Remove(committedGrant);
            await db.SaveChangesAsync();
            using (var revoked = await ReadAsync(
                client, tenantA.Id, branchA.Id, jwt))
                Assert.Equal(HttpStatusCode.Forbidden, revoked.StatusCode);

            // A rolled-back MySQL grant must not become visible to EF readers.
            await using (var sql = new MySqlConnection(scoped))
            {
                await sql.OpenAsync();
                await using var tx = await sql.BeginTransactionAsync();
                await using var insert = sql.CreateCommand();
                insert.Transaction = tx;
                insert.CommandText =
                    "INSERT INTO RolPermisos (RolId, PermisoId) VALUES (@role,@perm)";
                insert.Parameters.AddWithValue("@role", roleA.Id);
                insert.Parameters.AddWithValue("@perm", permission.Id);
                await insert.ExecuteNonQueryAsync();
                await tx.RollbackAsync();
            }
            using (var afterRollback = await ReadAsync(
                client, tenantA.Id, branchA.Id, jwt))
                Assert.Equal(HttpStatusCode.Forbidden, afterRollback.StatusCode);

            db.RolPermisos.Add(new RolPermiso
            {
                RolId = roleA.Id, PermisoId = permission.Id
            });
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
            var actualMembership = await db.UsuarioEmpresas
                .SingleAsync(x => x.UsuarioId == actor.Id &&
                    x.EmpresaId == tenantA.Id);
            actualMembership.Desactivar();
            await db.SaveChangesAsync();
            using (var disabledMembership = await ReadAsync(
                client, tenantA.Id, branchA.Id, jwt))
                Assert.Equal(HttpStatusCode.Forbidden,
                    disabledMembership.StatusCode);

            actualMembership.Activar();
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
            var assignedBranch = await db.Set<Sucursal>()
                .SingleAsync(x => x.Id == branchA.Id);
            assignedBranch.EmpresaId = tenantB.Id;
            await db.SaveChangesAsync();
            using (var movedBranch = await ReadAsync(
                client, tenantA.Id, branchA.Id, jwt))
                Assert.Equal(HttpStatusCode.Forbidden, movedBranch.StatusCode);
        }
        finally
        {
            // Only this random, ephemeral CI database is ever dropped.
            await using var cleanup = new AppDbContext(options);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }
}

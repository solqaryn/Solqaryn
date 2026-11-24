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
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Services;
using Solqaryn.Infrastructure.Repositories;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Services;
using Xunit;

namespace Solqaryn.Tests.Integration;

/// <summary>
/// C08-03.M03.03: actual DEV-only read source, signed HTTP bearer and Oracle EF
/// on an isolated MySQL 8.4 database. Never creates a production route or a
/// login payment, membership, door or hardware authorization.
/// </summary>
[Trait("Category", "Integration")]
public sealed class C0301M0303RealMysql84HttpIntegrationTests
{
    private const string BaseDatabase = "solqaryn_c1502_m0503";
    private const string Issuer = "c0803-m0303-test-only";
    private const string Audience = "c0803-m0303-client-only";
    private static readonly SymmetricSecurityKey Key = new(
        Encoding.UTF8.GetBytes("c0301-login-real-mysql84-signed-jwt-test-only-2026"));
    private static readonly SymmetricSecurityKey ForgedKey = new(
        Encoding.UTF8.GetBytes("c0301-login-real-mysql84-forged-jwt-test-only-2026"));

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

    private static string Token(int actor, bool forged = false, int? role = null)
    {
        var claims = new List<Claim> { new("id", actor.ToString(
            System.Globalization.CultureInfo.InvariantCulture)) };
        if (role.HasValue)
            claims.Add(new Claim("rolId", role.Value.ToString(
                System.Globalization.CultureInfo.InvariantCulture)));
        return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            issuer: Issuer, audience: Audience,
            claims: claims,
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
                ["LoginSecurity:EnableReadOnlyProbe"] = enableProbe ? "true" : "false",
                ["Jwt:Secret"] = "c0301-login-real-mysql84-signed-jwt-test-only-2026",
                ["Jwt:Issuer"] = Issuer,
                ["Jwt:Audience"] = Audience
            }).Build();
        return await new HostBuilder().ConfigureWebHost(web => web.UseTestServer()
            .ConfigureServices(services =>
            {
                services.AddSingleton<IConfiguration>(settings);
                services.AddRouting();
                services.AddControllers().AddApplicationPart(
                    typeof(Solqaryn.API.Controllers.AuthController).Assembly);
                services.AddScoped<IUsuarioRepository, UsuarioRepository>();
                services.AddScoped<IRolRepository, RolRepository>();
                services.AddScoped<IAuthService, AuthService>();
                services.AddScoped<IJwtService, JwtService>();
                services.AddScoped<ITenantSessionTokenService, JwtService>();
                // Refresh does not invoke auditing; this dependency is not a
                // substitute for a real audit integration.
                services.AddSingleton(Moq.Mock.Of<IAuditoriaService>());
                services.AddHttpContextAccessor();
                services.AddDbContext<AppDbContext>(options => options.UseMySQL(
                    connection, mysql => mysql.MigrationsAssembly(
                        "Solqaryn.Infrastructure.Migrations")));
                services.AddScoped<ICurrentUserService, CurrentUserService>();
                services.AddScoped<IUsuarioScopeService, UsuarioScopeService>();
                services.AddScoped<IC0301VerifiedLoginActorSource,
                    C0301LoginVerifiedActorSource>();
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
                    }).AddJwtBearer(TenantSessionToken.Scheme, options =>
                        TenantSessionBearer.Configure(options, Issuer, Audience, Key));
                services.AddAuthorization();
            }).Configure(app =>
            {
                app.UseRouting();
                app.UseAuthentication();
                app.UseAuthorization();
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapControllers();
                    if (enableProbe)
                        C0301LoginReadOnlyProbeEndpoint.Map(endpoints);
                });
            })).StartAsync();
    }

    private static async Task<HttpResponseMessage> ReadAsync(
        HttpClient client, int tenant, int branch, string? jwt)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get,
            $"/__dev/login-tenant-readonly/{tenant}/{branch}");
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
        var schema = "c0301_m0303_" + Guid.NewGuid().ToString("N");
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

            var tenantA = new Empresa("C0301 login isolated A");
            var tenantB = new Empresa("C0301 login isolated B");
            db.Set<Empresa>().AddRange(tenantA, tenantB);
            await db.SaveChangesAsync();

            var roleA = new Rol
            {
                Nombre = "C0301 login reader", NombreNormalizado = "C0301_GYM",
                Ambito = AmbitoAutorizacion.Empresa,
                EmpresaId = tenantA.Id, Activo = true
            };
            db.Roles.Add(roleA);
            await db.SaveChangesAsync();
            var actor = new Usuario
            {
                NombreUsuario = "login_" + Guid.NewGuid().ToString("N")[..12],
                NombreCompleto = "Login test actor",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("isolated-login-password"),
                RolId = roleA.Id, Activo = true
            };
            db.Usuarios.Add(actor);
            await db.SaveChangesAsync();
            var membership = new UsuarioEmpresa(actor.Id, tenantA.Id, roleA.Id);
            db.UsuarioEmpresas.Add(membership);
            var branchA = new Sucursal
            {
                EmpresaId = tenantA.Id, Codigo = "GYM-A",
                Nombre = "Login A", Activa = true
            };
            var branchB = new Sucursal
            {
                EmpresaId = tenantB.Id, Codigo = "GYM-B",
                Nombre = "Login B", Activa = true
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
                    Codigo = "C0301.SUCURSALES.VER",
                    Nombre = "C0301 isolated branch read",
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

            // Production AuthService and JWT signer on fresh Oracle EF contexts.
            // An active membership alone cannot issue or renew a token for an inactive tenant.
            var authSettings = new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Jwt:Secret"] = "c0301-login-real-mysql84-signed-jwt-test-only-2026",
                    ["Jwt:Issuer"] = Issuer,
                    ["Jwt:Audience"] = Audience
                }).Build();
            async Task AssertTokenGateAsync(bool allowed)
            {
                await using var authDb = new AppDbContext(options);
                var auth = new AuthService(new UsuarioRepository(authDb),
                    new RolRepository(authDb), new JwtService(authSettings));
                var login = await auth.LoginAsync(new LoginRequestDto
                {
                    NombreUsuario = actor.NombreUsuario,
                    Password = "isolated-login-password"
                });
                var renewal = await auth.RenovarAsync(actor.Id, roleA.Id);
                // An old signed role must not acquire the currently active role
                // even when the actor has a valid live SQL membership.
                Assert.Null(await auth.RenovarAsync(actor.Id, checked(roleA.Id + 1)));
                if (!allowed)
                {
                    Assert.Null(login);
                    Assert.Null(renewal);
                    return;
                }

                Assert.NotNull(login);
                Assert.NotNull(renewal);
                foreach (var issued in new[] { login.Token, renewal.Token })
                {
                    var principal = new JwtSecurityTokenHandler().ValidateToken(issued,
                        new TokenValidationParameters
                        {
                            ValidIssuer = Issuer, ValidAudience = Audience,
                            IssuerSigningKey = Key, ValidateIssuerSigningKey = true,
                            ValidateIssuer = true, ValidateAudience = true,
                            ValidateLifetime = true, RequireSignedTokens = true
                        }, out _);
                    Assert.Equal(actor.Id.ToString(), principal.FindFirst("id")?.Value);
                    Assert.Equal(roleA.Id.ToString(), principal.FindFirst("rolId")?.Value);
                }
            }

            await AssertTokenGateAsync(true);
            var liveCompany = await db.Set<Empresa>().SingleAsync(e => e.Id == tenantA.Id);
            liveCompany.Desactivar();
            await db.SaveChangesAsync();
            await AssertTokenGateAsync(false);
            liveCompany.Activar();
            await db.SaveChangesAsync();
            await AssertTokenGateAsync(true);

            using var host = await StartAsync(scoped);
            using var client = host.GetTestClient();
            var jwt = Token(actor.Id);
            string productToken;
            await using (var authDb = new AppDbContext(options))
            {
                var auth = new AuthService(new UsuarioRepository(authDb),
                    new RolRepository(authDb), new JwtService(authSettings));
                var login = await auth.LoginAsync(new LoginRequestDto
                    { NombreUsuario = actor.NombreUsuario, Password = "isolated-login-password" });
                Assert.NotNull(login);
                productToken = login.Token;
            }
            async Task<HttpResponseMessage> RefreshAsync(string token)
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, "/auth/renovar");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                return await client.SendAsync(request);
            }
            async Task<HttpResponseMessage> ContextAsync(string path, string token)
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, path);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                request.Headers.Add("X-Empresa-Id", tenantB.Id.ToString());
                return await client.SendAsync(request);
            }
            async Task<HttpResponseMessage> IssueContextAsync(string bearer, int tenant, int branch)
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, "/tenant-context/sesion");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
                request.Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(
                    new { empresaId = tenant, sucursalId = branch }), Encoding.UTF8, "application/json");
                return await client.SendAsync(request);
            }
            async Task<HttpResponseMessage> ReadContextAsync(string bearer, int? header = null)
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, "/tenant-context/sesion");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
                if (header.HasValue) request.Headers.Add("X-Empresa-Id", header.Value.ToString());
                return await client.SendAsync(request);
            }
            async Task<string> RequireIssuedAsync(int tenant, int branch, int role)
            {
                using var issued = await IssueContextAsync(productToken, tenant, branch);
                Assert.Equal(HttpStatusCode.OK, issued.StatusCode);
                using var json = System.Text.Json.JsonDocument.Parse(await issued.Content.ReadAsStringAsync());
                var data = json.RootElement.GetProperty("data");
                Assert.Equal(role, data.GetProperty("rolId").GetInt32());
                var restricted = data.GetProperty("token").GetString()!;
                var decoded = new JwtSecurityTokenHandler().ReadJwtToken(restricted);
                Assert.Equal(TenantSessionToken.Audience(Audience), Assert.Single(decoded.Audiences));
                Assert.True(decoded.ValidTo <= new JwtSecurityTokenHandler().ReadJwtToken(productToken).ValidTo);
                Assert.DoesNotContain(decoded.Claims, claim => claim.Type == "esAdministrador");
                Assert.Equal(role.ToString(), decoded.Claims.Single(claim => claim.Type == "tenantRoleId").Value);
                using var read = await ReadContextAsync(restricted);
                Assert.Equal(HttpStatusCode.OK, read.StatusCode);
                using var legacy = await RefreshAsync(restricted);
                Assert.Equal(HttpStatusCode.Unauthorized, legacy.StatusCode);
                return restricted;
            }
            var restrictedA = await RequireIssuedAsync(tenantA.Id, branchA.Id, roleA.Id);
            using (var baseOnly = await ReadContextAsync(productToken))
                Assert.Equal(HttpStatusCode.Unauthorized, baseOnly.StatusCode);
            using (var crossHeader = await ReadContextAsync(restrictedA, tenantB.Id))
                Assert.Equal(HttpStatusCode.Forbidden, crossHeader.StatusCode);
            using (var crossBranch = await IssueContextAsync(productToken, tenantA.Id, branchB.Id))
                Assert.Equal(HttpStatusCode.Forbidden, crossBranch.StatusCode);
            using (var noMembership = await IssueContextAsync(productToken, tenantB.Id, branchB.Id))
                Assert.Equal(HttpStatusCode.Forbidden, noMembership.StatusCode);
            async Task AssertContextAsync(HttpStatusCode expected)
            {
                using var response = await ContextAsync($"/tenant-context/{tenantA.Id}", productToken);
                Assert.Equal(expected, response.StatusCode);
                using var list = await ContextAsync("/tenant-context/empresas", productToken);
                Assert.Equal(expected, list.StatusCode);
                if (expected == HttpStatusCode.OK)
                {
                    using var json = System.Text.Json.JsonDocument.Parse(await list.Content.ReadAsStringAsync());
                    var data = Assert.Single(json.RootElement.GetProperty("data").EnumerateArray());
                    Assert.Equal(tenantA.Id, data.GetProperty("empresaId").GetInt32());
                }
            }
            await AssertContextAsync(HttpStatusCode.OK);
            using (var foreignContext = await ContextAsync($"/tenant-context/{tenantB.Id}", productToken))
                Assert.Equal(HttpStatusCode.Forbidden, foreignContext.StatusCode);

            // The signed base role remains A, but the destination B has a
            // distinct current membership/role. B revocation must not fall
            // back to A or resurrect B with the still-valid base token.
            var roleB = new Rol
            {
                Nombre = "C0301 login tenant B", NombreNormalizado = "C0301_LOGIN_B",
                Ambito = AmbitoAutorizacion.Empresa,
                EmpresaId = tenantB.Id, Activo = true
            };
            db.Roles.Add(roleB);
            await db.SaveChangesAsync();
            var membershipB = new UsuarioEmpresa(actor.Id, tenantB.Id, roleB.Id);
            db.UsuarioEmpresas.Add(membershipB);
            await db.SaveChangesAsync();
            using (var contextB = await ContextAsync($"/tenant-context/{tenantB.Id}", productToken))
            {
                Assert.Equal(HttpStatusCode.OK, contextB.StatusCode);
                Assert.True(contextB.Headers.CacheControl?.NoStore == true);
                using var json = System.Text.Json.JsonDocument.Parse(await contextB.Content.ReadAsStringAsync());
                Assert.Equal(tenantB.Id, json.RootElement.GetProperty("data").GetProperty("empresaId").GetInt32());
                Assert.Equal(roleB.Id, json.RootElement.GetProperty("data").GetProperty("rolId").GetInt32());
            }
            db.RolPermisos.Add(new RolPermiso { RolId = roleB.Id, PermisoId = permission.Id });
            await db.SaveChangesAsync();
            var restrictedB = await RequireIssuedAsync(tenantB.Id, branchB.Id, roleB.Id);
            membershipB.Desactivar();
            await db.SaveChangesAsync();
            using (var revokedRestricted = await ReadContextAsync(restrictedB))
                Assert.Equal(HttpStatusCode.Forbidden, revokedRestricted.StatusCode);
            using (var revokedB = await ContextAsync($"/tenant-context/{tenantB.Id}", productToken))
                Assert.Equal(HttpStatusCode.Forbidden, revokedB.StatusCode);
            await AssertContextAsync(HttpStatusCode.OK);
            using (var missingRoleContext = await ContextAsync("/tenant-context/empresas", jwt))
                Assert.Equal(HttpStatusCode.Unauthorized, missingRoleContext.StatusCode);
            using (var renewed = await RefreshAsync(productToken))
                Assert.Equal(HttpStatusCode.OK, renewed.StatusCode);
            using (var missingRole = await RefreshAsync(jwt))
                Assert.Equal(HttpStatusCode.Unauthorized, missingRole.StatusCode);
            using (var invalidSignature = await RefreshAsync(Token(actor.Id, forged: true)))
                Assert.Equal(HttpStatusCode.Unauthorized, invalidSignature.StatusCode);
            using (var anonymous = await ReadAsync(
                client, tenantA.Id, branchA.Id, null))
                Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
            using (var forged = await ReadAsync(
                client, tenantA.Id, branchA.Id, Token(actor.Id, forged: true)))
                Assert.Equal(HttpStatusCode.Unauthorized, forged.StatusCode);
            using (var wrongActor = await ReadAsync(
                client, tenantA.Id, branchA.Id, Token(actor.Id + 1, role: roleA.Id)))
                Assert.Equal(HttpStatusCode.Forbidden, wrongActor.StatusCode);
            using (var missingBaseRole = await ReadAsync(
                client, tenantA.Id, branchA.Id, jwt))
                Assert.Equal(HttpStatusCode.Forbidden, missingBaseRole.StatusCode);
            using (var wrongBaseRole = await ReadAsync(
                client, tenantA.Id, branchA.Id,
                Token(actor.Id, role: checked(roleA.Id + 1))))
                Assert.Equal(HttpStatusCode.Forbidden, wrongBaseRole.StatusCode);
            using (var foreignTenant = await ReadAsync(
                client, tenantB.Id, branchB.Id, productToken))
                Assert.Equal(HttpStatusCode.Forbidden, foreignTenant.StatusCode);
            using (var foreignBranch = await ReadAsync(
                client, tenantA.Id, branchB.Id, productToken))
                Assert.Equal(HttpStatusCode.Forbidden, foreignBranch.StatusCode);
            using (var positive = await ReadAsync(
                client, tenantA.Id, branchA.Id, productToken))
            {
                Assert.Equal(HttpStatusCode.OK, positive.StatusCode);
                Assert.Equal("LOGIN_SELECTION_ONLY_NO_TOKEN_OR_GRANT",
                    await positive.Content.ReadAsStringAsync());
            }

            // A signed token must not preserve a role disabled in current MySQL.
            var liveRole = await db.Roles.SingleAsync(r => r.Id == roleA.Id);
            liveRole.Activo = false;
            await db.SaveChangesAsync();
            using (var disabledRole = await ReadAsync(
                client, tenantA.Id, branchA.Id, productToken))
                Assert.Equal(HttpStatusCode.Forbidden, disabledRole.StatusCode);
            await AssertContextAsync(HttpStatusCode.Forbidden);
            liveRole.Activo = true;
            await db.SaveChangesAsync();
            await AssertContextAsync(HttpStatusCode.OK);

            var committedGrant = await db.RolPermisos
                .SingleAsync(g => g.RolId == roleA.Id &&
                    g.PermisoId == permission.Id);
            db.RolPermisos.Remove(committedGrant);
            await db.SaveChangesAsync();
            using (var revoked = await ReadAsync(
                client, tenantA.Id, branchA.Id, productToken))
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
                client, tenantA.Id, branchA.Id, productToken))
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
            using (var revokedRefresh = await RefreshAsync(productToken))
                Assert.Equal(HttpStatusCode.Unauthorized, revokedRefresh.StatusCode);
            using (var disabledMembership = await ReadAsync(
                client, tenantA.Id, branchA.Id, productToken))
                Assert.Equal(HttpStatusCode.Forbidden,
                    disabledMembership.StatusCode);

            await AssertContextAsync(HttpStatusCode.Forbidden);
            actualMembership.Activar();
            await db.SaveChangesAsync();
            await AssertContextAsync(HttpStatusCode.OK);
            db.ChangeTracker.Clear();
            var assignedBranch = await db.Set<Sucursal>()
                .SingleAsync(x => x.Id == branchA.Id);
            assignedBranch.EmpresaId = tenantB.Id;
            await db.SaveChangesAsync();
            using (var movedBranch = await ReadAsync(
                client, tenantA.Id, branchA.Id, productToken))
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

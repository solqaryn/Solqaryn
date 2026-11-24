using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using MySql.Data.MySqlClient;
using MySql.EntityFrameworkCore.Extensions;
using Solqaryn.API.Controllers;
using Solqaryn.Application.AutomationV5;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.AutomationV5;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Services;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// C02-01.M05.04: signed JWT + HTTP + Oracle EF/MySQL 8.4 positive tenant\n/// membership proof, with adversarial cases. The last preference fixture\n/// is NOT live per-user preference persistence or a completed UI redirect.
/// </summary>
[Trait("Category", "Integration")]
public sealed class C0201M0504SignedJwtMysqlPositiveTests
{
    private const string Issuer = "c0201-m0504-ci-issuer";
    private const string Audience = "c0201-m0504-ci-audience";
    private static readonly SymmetricSecurityKey Key = new(
        Encoding.UTF8.GetBytes("c0201-m0504-ephemeral-signed-token-key-2026-never-production"));

    private sealed class VerifiedHttpCurrentUser(IHttpContextAccessor access) : ICurrentUserService
    {
        public bool EstaAutenticado =>
            access.HttpContext?.User.Identity?.IsAuthenticated == true;
        public int? UsuarioId => int.TryParse(
            access.HttpContext?.User.FindFirst("id")?.Value, out var id) ? id : null;
        public string? NombreUsuario => null;
        public string? NombreCompleto => null;
        public int? RolId => null; // Never trust this JWT claim as an authority.
        public bool EsAdministrador => false;
    }

    private sealed class FixedAuthorityReader(
        V17IntelligentTenantLoginAuthorization.TrustedSnapshot snapshot)
        : ITrustedTenantLoginAuthorityReader
    {
        public ValueTask<V17IntelligentTenantLoginAuthorization.TrustedSnapshot?>
            ReadCurrentAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult<
                V17IntelligentTenantLoginAuthorization.TrustedSnapshot?>(snapshot);
        }
    }

    private sealed class MutableProbeCurrentUser(int actorId) : ICurrentUserService
    {
        public int? UsuarioId { get; set; } = actorId;
        public bool EstaAutenticado { get; set; } = true;
        public string? NombreUsuario => null;
        public string? NombreCompleto => null;
        public int? RolId => null;
        public bool EsAdministrador => false;
    }

    private sealed class ActorSwitchingScope(
        MutableProbeCurrentUser actor,
        int replacementActorId,
        EmpresaSeleccionable leakedCompany) : IUsuarioScopeService
    {
        public Task<UsuarioScopeActual?> ObtenerActualAsync() =>
            Task.FromResult<UsuarioScopeActual?>(null);

        public Task<IReadOnlyList<EmpresaSeleccionable>> ListarEmpresasSeleccionablesAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            // Adversarial provider changes identity after SQL verified original actor.
            actor.UsuarioId = replacementActorId;
            return Task.FromResult<IReadOnlyList<EmpresaSeleccionable>>([leakedCompany]);
        }
    }

    // The trusted scope returns its first real SQL list, revokes B in the
    // disposable database, and returns the now-stale snapshot on purpose.
    // A second genuine SQL list must no longer include B.
    private sealed class RevokingMembershipSqlScope(
        AppDbContext database, IUsuarioScopeService verifiedScope,
        int actorId, int tenantId) : IUsuarioScopeService
    {
        private bool _revoked;
        public Task<UsuarioScopeActual?> ObtenerActualAsync() =>
            verifiedScope.ObtenerActualAsync();

        public async Task<IReadOnlyList<EmpresaSeleccionable>>
            ListarEmpresasSeleccionablesAsync(
                CancellationToken cancellationToken = default)
        {
            var listed = await verifiedScope.ListarEmpresasSeleccionablesAsync(
                cancellationToken);
            if (!_revoked)
            {
                _revoked = true;
                var membership = await database.UsuarioEmpresas.SingleAsync(
                    m => m.UsuarioId == actorId && m.EmpresaId == tenantId,
                    cancellationToken);
                membership.Desactivar();
                await database.SaveChangesAsync(cancellationToken);
                database.ChangeTracker.Clear();
            }
            return listed;
        }
    }

    // Real disposable MySQL revocation after the initial active-user check.
    private sealed class RevokingSqlScope(
        AppDbContext database, int actorId, EmpresaSeleccionable candidate)
        : IUsuarioScopeService
    {
        public Task<UsuarioScopeActual?> ObtenerActualAsync() =>
            Task.FromResult<UsuarioScopeActual?>(null);

        public async Task<IReadOnlyList<EmpresaSeleccionable>>
            ListarEmpresasSeleccionablesAsync(
                CancellationToken cancellationToken = default)
        {
            var actor = await database.Usuarios.SingleAsync(
                u => u.Id == actorId, cancellationToken);
            actor.Activo = false;
            await database.SaveChangesAsync(cancellationToken);
            database.ChangeTracker.Clear();
            return [candidate];
        }
    }

    // Adversarial caching adapter: returns the exact same stale A+B list
    // twice while B's relational ROLE is revoked in genuine disposable MySQL.
    // The SQL-backed reader must reject it even though its scope snapshots match.
    private sealed class RevokingRoleCachedScope(
        AppDbContext database, IUsuarioScopeService trustedScope, int roleId)
        : IUsuarioScopeService
    {
        private IReadOnlyList<EmpresaSeleccionable>? _cached;
        public Task<UsuarioScopeActual?> ObtenerActualAsync() =>
            trustedScope.ObtenerActualAsync();

        public async Task<IReadOnlyList<EmpresaSeleccionable>>
            ListarEmpresasSeleccionablesAsync(
                CancellationToken cancellationToken = default)
        {
            if (_cached is not null) return _cached;
            _cached = await trustedScope.ListarEmpresasSeleccionablesAsync(
                cancellationToken);
            if (_cached.Count != 2)
                throw new InvalidOperationException("REAL_SQL_AB_REQUIRED");
            var role = await database.Roles.SingleAsync(
                r => r.Id == roleId, cancellationToken);
            role.Activo = false;
            await database.SaveChangesAsync(cancellationToken);
            database.ChangeTracker.Clear();
            return _cached;
        }
    }

    private static async Task<(string Code, int? TenantId)> ReadDevResolutionAsync(
        HttpClient client)
    {
        using var response = await client.GetAsync("/tenant-context/resolver-login");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;
        Assert.False(root.GetProperty("issuesJwt").GetBoolean());
        Assert.False(root.GetProperty("authorizesResourceAccess").GetBoolean());
        Assert.False(root.GetProperty("installsTenantContext").GetBoolean());
        var tenant = root.GetProperty("proposedTenantId");
        return (root.GetProperty("code").GetString()!,
            tenant.ValueKind == JsonValueKind.Null ? null : tenant.GetInt32());
    }

    private static async Task<(string Code, int? TenantId)> ReadSqlPolicyOverJwtAsync(
        HttpClient client)
    {
        using var response = await client.GetAsync("/test-only/c0201-m0504/sql-decision");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;
        Assert.False(root.GetProperty("issuesJwt").GetBoolean());
        Assert.False(root.GetProperty("authorizesResourceAccess").GetBoolean());
        var tenant = root.GetProperty("proposedTenantId");
        return (root.GetProperty("code").GetString()!,
            tenant.ValueKind == JsonValueKind.Null ? null : tenant.GetInt32());
    }

    [Fact]
    public async Task SignedJwtPositiveTenantSelectionAndPreferenceRequireBackendReissue()
    {
        var raw = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? throw new InvalidOperationException("EPHEMERAL_MYSQL84_REQUIRED");
        var source = new MySqlConnectionStringBuilder(raw);
        if (source.Server is not ("127.0.0.1" or "localhost") ||
            source.Port != 3306 || source.UserID != "root")
            throw new InvalidOperationException("EPHEMERAL_LOOPBACK_ONLY");

        var isolated = new MySqlConnectionStringBuilder(raw)
        {
            Database = "c0201_m0504_" + Guid.NewGuid().ToString("N")
        }.ConnectionString;
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL(isolated,
                m => m.MigrationsAssembly("Solqaryn.Infrastructure.Migrations"))
            .Options;

        try
        {
            await using var db = new AppDbContext(options);
            await Phase7MySqlTestDatabase.InitializeFreshAsync(db);

            var a = new Empresa("C0201 M0504 Tenant A");
            var b = new Empresa("C0201 M0504 Tenant B");
            db.Set<Empresa>().AddRange(a, b);
            await db.SaveChangesAsync();
            var roleA = new Rol
            {
                Nombre = "C0201 M0504 role A", NombreNormalizado = "M0504_A",
                EmpresaId = a.Id, Ambito = AmbitoAutorizacion.Empresa, Activo = true
            };
            var roleB = new Rol
            {
                Nombre = "C0201 M0504 role B", NombreNormalizado = "M0504_B",
                EmpresaId = b.Id, Ambito = AmbitoAutorizacion.Empresa, Activo = true
            };
            db.Roles.AddRange(roleA, roleB);
            await db.SaveChangesAsync();
            var user = new Usuario
            {
                NombreUsuario = "m0504_" + Guid.NewGuid().ToString("N")[..12],
                NombreCompleto = "Isolated integration actor",
                PasswordHash = "never-a-real-password", RolId = roleA.Id,
                Activo = true
            };
            db.Usuarios.Add(user);
            await db.SaveChangesAsync();
            db.UsuarioEmpresas.AddRange(
                new UsuarioEmpresa(user.Id, a.Id, roleA.Id),
                new UsuarioEmpresa(user.Id, b.Id, roleB.Id));
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            // Real disposable MySQL actor lookup plus adversarial scope change.
            // Old reader would attach the replacement actor's tenant to A.
            var mutableActor = new MutableProbeCurrentUser(user.Id);
            var switchedReader = new C0201M0504CurrentMembershipSqlReader(
                db, mutableActor,
                new ActorSwitchingScope(mutableActor, user.Id + 100000,
                    new EmpresaSeleccionable(b.Id, b.Nombre)));
            var crossActorSnapshot = await switchedReader.ReadCurrentAsync(
                CancellationToken.None);
            Assert.Null(crossActorSnapshot);
            Assert.Equal(user.Id + 100000, mutableActor.UsuarioId);

            // A scope may return a valid tenant after the actor was revoked.
            // The second real EF/MySQL read must now deny the entire snapshot.
            var revokedReader = new C0201M0504CurrentMembershipSqlReader(
                db, new MutableProbeCurrentUser(user.Id),
                new RevokingSqlScope(db, user.Id,
                    new EmpresaSeleccionable(a.Id, a.Nombre)));
            Assert.Null(await revokedReader.ReadCurrentAsync(
                CancellationToken.None));
            var reactivated = await db.Usuarios.SingleAsync(
                u => u.Id == user.Id);
            Assert.False(reactivated.Activo);
            reactivated.Activo = true;
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            // The initial selector legitimately sees A+B. B is revoked in
            // real MySQL before the reader returns: the stale B must not
            // survive a second SQL membership/role/tenant authority read.
            var membershipActor = new MutableProbeCurrentUser(user.Id);
            var liveScope = new UsuarioScopeService(db, membershipActor);
            var staleMembershipReader = new C0201M0504CurrentMembershipSqlReader(
                db, membershipActor,
                new RevokingMembershipSqlScope(db, liveScope, user.Id, b.Id));
            Assert.Null(await staleMembershipReader.ReadCurrentAsync(
                CancellationToken.None));
            var revokedMembership = await db.UsuarioEmpresas.SingleAsync(
                m => m.UsuarioId == user.Id && m.EmpresaId == b.Id);
            Assert.False(revokedMembership.Activa);
            revokedMembership.Activar();
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            // Role B is revoked AFTER real A+B SQL enumeration, but a
            // deliberately cached scope returns A+B again. This test failed
            // against the previous two-scope-list-only implementation.
            var cachedActor = new MutableProbeCurrentUser(user.Id);
            var cachedRoleReader = new C0201M0504CurrentMembershipSqlReader(
                db, cachedActor,
                new RevokingRoleCachedScope(
                    db, new UsuarioScopeService(db, cachedActor), roleB.Id));
            Assert.Null(await cachedRoleReader.ReadCurrentAsync(
                CancellationToken.None));
            var revokedRoleB = await db.Roles.SingleAsync(
                r => r.Id == roleB.Id);
            Assert.False(revokedRoleB.Activo);
            revokedRoleB.Activo = true;
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            using var host = await new HostBuilder()
                .ConfigureWebHost(web => web.UseEnvironment("Development").UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services.AddDbContext<AppDbContext>(o =>
                            o.UseMySQL(isolated, m =>
                                m.MigrationsAssembly("Solqaryn.Infrastructure.Migrations")));
                        services.AddHttpContextAccessor();
                        services.AddScoped<ICurrentUserService, VerifiedHttpCurrentUser>();
                        services.AddScoped<IUsuarioScopeService, UsuarioScopeService>();
                        services.AddScoped<ITrustedTenantLoginAuthorityReader,
                            C0201M0504CurrentMembershipSqlReader>();
                        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                            .AddJwtBearer(o => o.TokenValidationParameters =
                                new TokenValidationParameters
                                {
                                    ValidateIssuer = true, ValidateAudience = true,
                                    ValidateLifetime = true,
                                    ValidateIssuerSigningKey = true,
                                    ValidIssuer = Issuer, ValidAudience = Audience,
                                    IssuerSigningKey = Key, ClockSkew = TimeSpan.Zero
                                });
                        services.AddAuthorization();
                        services.AddControllers().AddApplicationPart(
                            typeof(TenantContextController).Assembly);
                    })
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseAuthentication();
                        app.UseAuthorization();
                        app.UseEndpoints(routes =>
                        {
                            routes.MapControllers();
                            routes.MapGet("/test-only/c0201-m0504/sql-decision", async context =>
                            {
                                var reader = context.RequestServices
                                    .GetRequiredService<ITrustedTenantLoginAuthorityReader>();
                                var result = await new V17IntelligentTenantLoginAuthorization(reader)
                                    .ResolveAsync(new(), context.RequestAborted);
                                await context.Response.WriteAsJsonAsync(new
                                {
                                    result.Code, result.ProposedTenantId,
                                    result.IssuesJwt, result.AuthorizesResourceAccess
                                });
                            }).RequireAuthorization();
                        });
                    }))
                .StartAsync();
            using var client = host.GetTestClient();

            // The isolated probe exists only in this TestServer, not production.
            // It is JWT-authorized and calls a REAL SQL-backed current-reader.
            using (var unsignedProbe = await client.GetAsync(
                "/test-only/c0201-m0504/sql-decision"))
                Assert.Equal(HttpStatusCode.Unauthorized, unsignedProbe.StatusCode);
            using (var unsignedDev = await client.GetAsync("/tenant-context/resolver-login"))
                Assert.Equal(HttpStatusCode.Unauthorized, unsignedDev.StatusCode);

            // Anonymous and a tampered signature never reach tenant membership.
            using (var anonymous = await client.GetAsync("/tenant-context/empresas"))
                Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
            var signed = Sign(user.Id, roleA.Id);
            var parts = signed.Split('.');
            parts[2] = (parts[2][0] == 'A' ? "B" : "A") + parts[2][1..];
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", string.Join(".", parts));
            using (var forgedJwt = await client.GetAsync("/tenant-context/" + a.Id))
                Assert.Equal(HttpStatusCode.Unauthorized, forgedJwt.StatusCode);
            using (var forgedDev = await client.GetAsync("/tenant-context/resolver-login"))
                Assert.Equal(HttpStatusCode.Unauthorized, forgedDev.StatusCode);

            // A valid signature does not make an unrelated or stale base role valid.
            // Preserve the previous fabricated role as a negative, and verify that
            // the actual role of another tenant cannot replace the actor base role.
            foreach (var invalidBaseRole in new[] { 999999, roleB.Id })
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", Sign(user.Id, invalidBaseRole));
                using var deniedContext = await client.GetAsync("/tenant-context/" + a.Id);
                Assert.Equal(HttpStatusCode.Forbidden, deniedContext.StatusCode);
                using var deniedList = await client.GetAsync("/tenant-context/empresas");
                Assert.Equal(HttpStatusCode.Forbidden, deniedList.StatusCode);
                using var deniedDev = await client.GetAsync("/tenant-context/resolver-login");
                Assert.Equal(HttpStatusCode.Forbidden, deniedDev.StatusCode);
                var deniedDevBody = await deniedDev.Content.ReadAsStringAsync();
                Assert.DoesNotContain(a.Nombre, deniedDevBody, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain(b.Nombre, deniedDevBody, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain(a.Nombre, await deniedContext.Content.ReadAsStringAsync(),
                    StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain(b.Nombre, await deniedList.Content.ReadAsStringAsync(),
                    StringComparison.OrdinalIgnoreCase);
            }

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", signed);
            client.DefaultRequestHeaders.TryAddWithoutValidation("X-Empresa-Id",
                b.Id.ToString());
            client.DefaultRequestHeaders.TryAddWithoutValidation(
                "X-Forwarded-Host", "other-tenant.attacker.invalid");
            using (var authorizedA = await client.GetAsync("/tenant-context/" + a.Id))
            {
                Assert.Equal(HttpStatusCode.OK, authorizedA.StatusCode);
                using var json = JsonDocument.Parse(
                    await authorizedA.Content.ReadAsStringAsync());
                var data = json.RootElement.GetProperty("data");
                Assert.Equal(a.Id, data.GetProperty("empresaId").GetInt32());
                Assert.Equal(roleA.Id, data.GetProperty("rolId").GetInt32());
                Assert.Equal(user.Id, data.GetProperty("usuarioId").GetInt32());
            }
            // The signed-JWT backend returns both active memberships from the real
            // Oracle EF/MySQL 8.4 store, never from a spoofed browser header.
            using (var validList = await client.GetAsync("/tenant-context/empresas"))
            {
                Assert.Equal(HttpStatusCode.OK, validList.StatusCode);
                using var json = JsonDocument.Parse(await validList.Content.ReadAsStringAsync());
                var valid = json.RootElement.GetProperty("data").EnumerateArray().ToArray();
                Assert.Equal(2, valid.Length);
                Assert.Contains(valid, tenant => tenant.GetProperty("empresaId").GetInt32() == a.Id);
                Assert.Contains(valid, tenant => tenant.GetProperty("empresaId").GetInt32() == b.Id);
            }
            var twoSql = await ReadSqlPolicyOverJwtAsync(client);
            Assert.Equal("SELECT_VERIFIED_TENANT", twoSql.Code);
            Assert.Null(twoSql.TenantId); // Two SQL memberships => no silent switch.
            var twoDev = await ReadDevResolutionAsync(client);
            Assert.Equal("SELECT_VERIFIED_TENANT", twoDev.Code);
            Assert.Null(twoDev.TenantId);

            using (var validB = await client.GetAsync("/tenant-context/" + b.Id))
            {
                Assert.Equal(HttpStatusCode.OK, validB.StatusCode);
                using var json = JsonDocument.Parse(await validB.Content.ReadAsStringAsync());
                Assert.Equal(b.Id, json.RootElement.GetProperty("data").GetProperty("empresaId").GetInt32());
                Assert.Equal(roleB.Id, json.RootElement.GetProperty("data").GetProperty("rolId").GetInt32());
            }
            // Positive LAST-VALID decision only: preference provenance is a
            // contract fixture, not production preference SQL or a JWT reissue.
            var preferred = new V17IntelligentTenantLoginAuthorization.TrustedSnapshot(
                user.Id, true, true, true, false, false, null,
                b.Id, user.Id, true, false,
                [
                    new(user.Id, a.Id, "Tenant A", true, true, true, true, false),
                    new(user.Id, b.Id, "Tenant B", true, true, true, true, false)
                ]);
            var preferredDecision = await new V17IntelligentTenantLoginAuthorization(
                new FixedAuthorityReader(preferred)).ResolveAsync(new());
            Assert.Equal("PREFERENCE_REISSUE_REQUIRED", preferredDecision.Code);
            Assert.Equal(b.Id, preferredDecision.ProposedTenantId);
            Assert.True(preferredDecision.RequiresAuthenticatedBackendReissue);
            Assert.False(preferredDecision.IssuesJwt);
            Assert.False(preferredDecision.AuthorizesResourceAccess);

            // Another signed actor with zero persisted memberships cannot borrow A.
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", Sign(user.Id + 100000, roleA.Id));
            using (var foreignActor = await client.GetAsync("/tenant-context/" + a.Id))
                Assert.Equal(HttpStatusCode.Forbidden, foreignActor.StatusCode);

            var foreignSql = await ReadSqlPolicyOverJwtAsync(client);
            Assert.Equal("AUTHENTICATED_CURRENT_MEMBERSHIPS_REQUIRED",
                foreignSql.Code);
            Assert.Null(foreignSql.TenantId);
            using (var foreignDev = await client.GetAsync("/tenant-context/resolver-login"))
            {
                Assert.Equal(HttpStatusCode.Forbidden, foreignDev.StatusCode);
                var body = await foreignDev.Content.ReadAsStringAsync();
                Assert.DoesNotContain(a.Nombre, body, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain(b.Nombre, body, StringComparison.OrdinalIgnoreCase);
            }

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", signed);
            var memberB = await db.UsuarioEmpresas.SingleAsync(m =>
                m.UsuarioId == user.Id && m.EmpresaId == b.Id);
            memberB.Desactivar();
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
            using (var revokedB = await client.GetAsync("/tenant-context/" + b.Id))
            {
                Assert.Equal(HttpStatusCode.Forbidden, revokedB.StatusCode);
                Assert.DoesNotContain("C0201 M0504 Tenant B",
                    await revokedB.Content.ReadAsStringAsync(),
                    StringComparison.OrdinalIgnoreCase);
            }
            using (var list = await client.GetAsync("/tenant-context/empresas"))
            {
                Assert.Equal(HttpStatusCode.OK, list.StatusCode);
                using var json = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
                var data = json.RootElement.GetProperty("data").EnumerateArray().ToArray();
                Assert.Single(data);
                Assert.Equal(a.Id, data[0].GetProperty("empresaId").GetInt32());
            }
            var oneSql = await ReadSqlPolicyOverJwtAsync(client);
            Assert.Equal("ONE_TENANT_REISSUE_REQUIRED", oneSql.Code);
            Assert.Equal(a.Id, oneSql.TenantId);
            var oneDev = await ReadDevResolutionAsync(client);
            Assert.Equal("ONE_TENANT_REISSUE_REQUIRED", oneDev.Code);
            Assert.Equal(a.Id, oneDev.TenantId);

            // After B is revoked in real MySQL, only A remains eligible.
            // A fresh tenant snapshot must require backend reissue, not install
            // a client-side context or bypass navigation guards.
            // A3: derive the positive single-tenant policy input from the
            // actual Oracle EF/MySQL memberships AFTER the B revocation.
            // Do not fabricate an active tenant from a fixed in-memory list.
            // This remains a test harness, not a registered production reader,
            // durable user preference or JWT reissue endpoint.
            var persistedSingleTenantMemberships = await (
                from membership in db.UsuarioEmpresas.AsNoTracking()
                join company in db.Set<Empresa>().AsNoTracking()
                    on membership.EmpresaId equals company.Id
                join effectiveRole in db.Roles.AsNoTracking()
                    on membership.RolId equals effectiveRole.Id
                where membership.UsuarioId == user.Id &&
                      membership.Activa && company.Activa &&
                      effectiveRole.Activo && !effectiveRole.Eliminado &&
                      effectiveRole.Ambito == AmbitoAutorizacion.Empresa &&
                      (effectiveRole.EmpresaId == company.Id ||
                       (effectiveRole.EmpresaId == null && effectiveRole.EsSistema))
                select new { membership.EmpresaId, company.Nombre }
            ).ToListAsync();
            Assert.Single(persistedSingleTenantMemberships);
            Assert.Equal(a.Id, persistedSingleTenantMemberships[0].EmpresaId);
            Assert.DoesNotContain(persistedSingleTenantMemberships,
                membership => membership.EmpresaId == b.Id);
            var single = new V17IntelligentTenantLoginAuthorization.TrustedSnapshot(
                user.Id, true, true, true, false, false, null,
                null, null, false, false,
                persistedSingleTenantMemberships
                    .Select(membership =>
                        new V17IntelligentTenantLoginAuthorization.Membership(
                            user.Id, membership.EmpresaId, membership.Nombre,
                            true, true, true, true, false))
                    .ToArray());
            var singleDecision = await new V17IntelligentTenantLoginAuthorization(
                new FixedAuthorityReader(single)).ResolveAsync(new());
            Assert.Equal("ONE_TENANT_REISSUE_REQUIRED", singleDecision.Code);
            Assert.Equal(a.Id, singleDecision.ProposedTenantId);
            Assert.Empty(singleDecision.Choices);
            Assert.True(singleDecision.RequiresAuthenticatedBackendReissue);
            Assert.False(singleDecision.InstallsTenantContext);
            Assert.False(singleDecision.AuthorizesResourceAccess);

            var memberA = await db.UsuarioEmpresas.SingleAsync(m =>
                m.UsuarioId == user.Id && m.EmpresaId == a.Id);
            memberA.CambiarRol(roleB.Id); // Role belonging to B cannot authorize A.
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
            using (var stolenRole = await client.GetAsync("/tenant-context/" + a.Id))
                Assert.Equal(HttpStatusCode.Forbidden, stolenRole.StatusCode);

            using (var revokedDev = await client.GetAsync("/tenant-context/resolver-login"))
                Assert.Equal(HttpStatusCode.Forbidden, revokedDev.StatusCode);

            var revokedRoleSql = await ReadSqlPolicyOverJwtAsync(client);
            Assert.Equal("NO_AUTHORIZED_ENTERPRISE", revokedRoleSql.Code);
            Assert.Null(revokedRoleSql.TenantId);

            // The real Application preference policy rejects a different user's
            // preference. This is synthetic evidence, NOT live preference SQL.
            var snapshot = new V17IntelligentTenantLoginAuthorization.TrustedSnapshot(
                user.Id, true, true, true, false, false, null,
                b.Id, user.Id + 100000, true, false,
                [
                    new(user.Id, a.Id, "Tenant A", true, true, true, true, false),
                    new(user.Id, b.Id, "Tenant B", true, true, true, true, false)
                ]);
            var decision = await new V17IntelligentTenantLoginAuthorization(
                new FixedAuthorityReader(snapshot)).ResolveAsync(new());
            Assert.Equal("SELECT_VERIFIED_TENANT", decision.Code);
            Assert.True(decision.DiscardStalePreference);
            Assert.Null(decision.ProposedTenantId);
            Assert.False(decision.AuthorizesResourceAccess);
        }
        finally
        {
            await using var cleanup = new AppDbContext(options);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }

    private static string Sign(int id, int baseRoleId)
    {
        var jwt = new JwtSecurityToken(
            issuer: Issuer, audience: Audience,
            claims:
            [
                new Claim("id", id.ToString()),
                new Claim("rolId", baseRoleId.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                new Claim("esAdministrador", "true")
            ],
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: new SigningCredentials(Key,
                SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }
}

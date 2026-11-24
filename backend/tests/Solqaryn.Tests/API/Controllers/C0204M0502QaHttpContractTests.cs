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
using Solqaryn.API.Middleware;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Services;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// C02-04.M05.02: HTTP TestServer + JwtBearer real, controlador real, grants
/// persistidos y revocación en cada petición; MySQL 8.4 efímero en la CI dirigida.
/// Las respuestas GitHub son fixtures: la CI pública real se acredita aparte por SHA.
/// </summary>
public sealed class C0204M0502QaHttpContractTests
{
    private const string Sha = "74713a5868585223272c07074b474d994b6d3c60";
    private const string Path = "/plataforma/gestion/qa/evidencias/" + Sha;
    private const string Issuer = "solqaryn-c0204-tests";
    private const string Audience = "solqaryn-c0204-tests";
    private static readonly SymmetricSecurityKey TestKey = new(
        Encoding.UTF8.GetBytes("solqaryn-c0204-http-test-jwt-signed-only-for-ci-2026"));

    [Fact]
    public async Task AutoridadGlobalPorJwtYGrantPersistidoLeeSoloChecksRealesTipados()
    {
        var upstream = new GitHubFake(Payload());
        await using var fixture = await CrearAsync(upstream);
        using var client = fixture.Host.GetTestClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer",
                Token(fixture.GlobalId, "operadorglobal"));
        client.DefaultRequestHeaders.Add("X-Empresa-Id", "999999");
        using var response = await client.GetAsync(Path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("no-store", response.Headers.CacheControl?.ToString() ?? "");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var dto = json.RootElement.GetProperty("data");
        Assert.Equal(Sha, dto.GetProperty("sha").GetString());
        Assert.Equal("CI_MINIMA_ACREDITADA", dto.GetProperty("estadoCi").GetString());
        Assert.Equal("SIN_VERIFICAR", dto.GetProperty("estadoA5").GetString());
        Assert.Equal(4, dto.GetProperty("totalChecks").GetInt32());
        Assert.Equal(4, dto.GetProperty("checksExitosos").GetInt32());
        Assert.Equal(4, dto.GetProperty("checks").GetArrayLength());
        Assert.Equal(1, upstream.Calls);
        Assert.Equal("api.github.com", upstream.LastHost);

        using var scope = fixture.Host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var audit = Assert.Single(await db.Set<RegistroAuditoria>().ToListAsync());
        Assert.Equal(AmbitoAutorizacion.Plataforma, audit.AmbitoAutoridad);
        Assert.Null(audit.EmpresaId);
        Assert.DoesNotContain("999999", audit.Descripcion);
        Assert.NotNull(audit.ValoresNuevos);
        using var metadata = JsonDocument.Parse(audit.ValoresNuevos!);
        Assert.Equal("qa-check-evidence/v1", metadata.RootElement.GetProperty("Schema").GetString());
        Assert.Equal(Sha, metadata.RootElement.GetProperty("Sha").GetString());
        Assert.Equal("CI_MINIMA_ACREDITADA", metadata.RootElement.GetProperty("EstadoCi").GetString());
        Assert.Equal("SIN_VERIFICAR", metadata.RootElement.GetProperty("EstadoA5").GetString());
        Assert.Equal(4, metadata.RootElement.GetProperty("TotalChecks").GetInt32());
        Assert.Equal(4, metadata.RootElement.GetProperty("ChecksExitosos").GetInt32());
        Assert.DoesNotContain("private-secret", audit.ValoresNuevos!);
        Assert.True(Guid.TryParseExact(audit.CorrelationId, "N", out _));
    }

    [Fact]
    public async Task SinJwtYOtroAdminEmpresarialNoConsultaGitHubNiEvidenciaGlobal()
    {
        var upstream = new GitHubFake(Payload());
        await using var fixture = await CrearAsync(upstream);
        using var client = fixture.Host.GetTestClient();
        using (var anon = await client.GetAsync(Path))
            Assert.Equal(HttpStatusCode.Unauthorized, anon.StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", Token(fixture.EmpresaAdminId, "adminempresa", "true"));
        client.DefaultRequestHeaders.Add("X-Empresa-Id", "20");
        using (var enterprise = await client.GetAsync(Path))
            Assert.Equal(HttpStatusCode.Forbidden, enterprise.StatusCode);
        Assert.Equal(0, upstream.Calls);
    }

    [Fact]
    public async Task RevocacionGlobalTieneEfectoEnLaSiguientePeticionHttp()
    {
        var upstream = new GitHubFake(Payload());
        await using var fixture = await CrearAsync(upstream);
        using var client = fixture.Host.GetTestClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", Token(fixture.GlobalId, "operadorglobal"));
        using (var success = await client.GetAsync(Path))
            Assert.Equal(HttpStatusCode.OK, success.StatusCode);
        using (var scope = fixture.Host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var grant = await db.Set<UsuarioRolPlataforma>().SingleAsync(
                g => g.UsuarioId == fixture.GlobalId);
            grant.Desactivar();
            await db.SaveChangesAsync();
        }
        using (var revoked = await client.GetAsync(Path))
            Assert.Equal(HttpStatusCode.Forbidden, revoked.StatusCode);
        Assert.Equal(1, upstream.Calls);
    }

    [Fact]
    public async Task ShaMalformadoYErrorDeProveedorFallanCerradosSinLeaks()
    {
        var upstream = new GitHubFake(Payload());
        await using var fixture = await CrearAsync(upstream);
        using var client = fixture.Host.GetTestClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", Token(fixture.GlobalId, "operadorglobal"));
        using (var invalid = await client.GetAsync(
            "/plataforma/gestion/qa/evidencias/not-a-sha"))
            Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal(0, upstream.Calls);

        upstream.StatusCode = HttpStatusCode.Forbidden;
        using var unavailable = await client.GetAsync(Path);
        var body = await unavailable.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.ServiceUnavailable, unavailable.StatusCode);
        Assert.DoesNotContain("private-secret", body);
        Assert.Contains("no-store", unavailable.Headers.CacheControl?.ToString() ?? "");
    }

    [Fact]
    public async Task FalloOCheckRequiredOmitidoNoSeConvierteEnPassNiA5()
    {
        var upstream = new GitHubFake(Payload("failure"));
        await using var fixture = await CrearAsync(upstream);
        using var client = fixture.Host.GetTestClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", Token(fixture.GlobalId, "operadorglobal"));
        using (var failure = await client.GetAsync(Path))
        {
            Assert.Equal(HttpStatusCode.OK, failure.StatusCode);
            using var j = JsonDocument.Parse(await failure.Content.ReadAsStringAsync());
            Assert.Equal("CI_FALLIDA", j.RootElement.GetProperty("data")
                .GetProperty("estadoCi").GetString());
            Assert.Equal("SIN_VERIFICAR", j.RootElement.GetProperty("data")
                .GetProperty("estadoA5").GetString());
        }
        upstream.Payload = Payload("skipped");
        using (var skipped = await client.GetAsync(Path))
        {
            using var j = JsonDocument.Parse(await skipped.Content.ReadAsStringAsync());
            Assert.Equal("SIN_VERIFICAR", j.RootElement.GetProperty("data")
                .GetProperty("estadoCi").GetString());
        }
    }

    [Fact]
    public async Task GitHubChunkedSinContentLengthRechazaMasDe512000BytesSinExponerDatos()
    {
        var upstream = new GitHubFake(Payload()) { OmitContentLength = true };
        await using var fixture = await CrearAsync(upstream);
        using var client = fixture.Host.GetTestClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", Token(fixture.GlobalId, "operadorglobal"));

        using (var validChunked = await client.GetAsync(Path))
        {
            Assert.Equal(HttpStatusCode.OK, validChunked.StatusCode);
            using var parsed = JsonDocument.Parse(await validChunked.Content.ReadAsStringAsync());
            Assert.Equal("CI_MINIMA_ACREDITADA",
                parsed.RootElement.GetProperty("data").GetProperty("estadoCi").GetString());
        }

        // Valid JSON, but exceeds the strict response byte ceiling. A missing
        // Content-Length must not bypass the limit, regardless of MaxDepth.
        upstream.Payload = Payload()[..^1] + ",\"padding\":\"" + new string('x', 512_000) + "\"}";
        using (var oversizedChunked = await client.GetAsync(Path))
        {
            Assert.Equal(HttpStatusCode.ServiceUnavailable, oversizedChunked.StatusCode);
            Assert.Contains("no-store",
                oversizedChunked.Headers.CacheControl?.ToString() ?? "");
            var safeBody = await oversizedChunked.Content.ReadAsStringAsync();
            Assert.DoesNotContain("padding", safeBody);
            Assert.DoesNotContain("private-secret", safeBody);
        }

        Assert.Equal(2, upstream.Calls);
        using var scope = fixture.Host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var audits = await db.Set<RegistroAuditoria>().ToListAsync();
        Assert.Equal(2, audits.Count);
        Assert.Single(audits, a => a.Resultado == "Exito");
        var failed = Assert.Single(audits, a => a.Resultado == "Error");
        Assert.Equal("QA_UPSTREAM_SIZE", failed.Error);
        Assert.DoesNotContain("padding", failed.ValoresNuevos!);
    }

    [Fact]
    public async Task CatalogoGlobalRolesRechazaEmpresaYConservaAuditoria()
    {
        var upstream = new GitHubFake(Payload());
        await using var fixture = await CrearAsync(upstream);
        using var client = fixture.Host.GetTestClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", Token(fixture.GlobalId, "operadorglobal"));
        using (var allowed = await client.GetAsync("/plataforma/gestion/roles"))
        {
            Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
            var body = await allowed.Content.ReadAsStringAsync();
            Assert.Contains("\"success\":true", body);
        }
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", Token(fixture.EmpresaAdminId, "adminempresa", "true"));
        using (var denied = await client.GetAsync("/plataforma/gestion/roles"))
            Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        Assert.Equal(0, upstream.Calls);
    }

    // En el workflow dirigido se establece SOLQARYN_C0204_MYSQL_TEST_CONNECTION.
    // Sin esa variable las suites generales ejercitan TestServer/InMemory; su
    // PASS no constituye certificación MySQL. Jamás usar un host remoto.
    [Fact]
    public async Task Mysql84LocalJwtHttpRevocacionSinDatosComerciales()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(
                "SOLQARYN_C0204_MYSQL_TEST_CONNECTION")))
            return;

        var upstream = new GitHubFake(Payload(detailsUrls: new[]
        {
            "https://github.com/solqaryn/Solqaryn/actions/runs/123/job/456",
            "https://github.com/solqaryn/Solqaryn/actions/runs/123?token=private-secret",
            "https://private-secret@github.com/solqaryn/Solqaryn/actions/runs/123",
            "https://github.com:8443/solqaryn/Solqaryn/actions/runs/123"
        }));
        await using var fixture = await CrearAsync(upstream, mysql: true);
        using var client = fixture.Host.GetTestClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", Token(fixture.GlobalId, "operadorglobal"));
        const string goodCorrelation = "qa-global-success.20261124";
        client.DefaultRequestHeaders.Add(CorrelationIdMiddleware.HeaderName, goodCorrelation);
        using (var good = await client.GetAsync(Path))
        {
            Assert.Equal(HttpStatusCode.OK, good.StatusCode);
            Assert.Equal(goodCorrelation, Assert.Single(good.Headers.GetValues(CorrelationIdMiddleware.HeaderName)));
            var body = await good.Content.ReadAsStringAsync();
            Assert.DoesNotContain("private-secret", body);
            using var document = JsonDocument.Parse(body);
            var checks = document.RootElement.GetProperty("data").GetProperty("checks");
            Assert.Equal(4, checks.GetArrayLength());
            Assert.Equal("https://github.com/solqaryn/Solqaryn/actions/runs/123/job/456",
                checks[0].GetProperty("urlEvidencia").GetString());
            for (var i = 1; i < checks.GetArrayLength(); i++)
                Assert.False(checks[i].TryGetProperty("urlEvidencia", out var url) &&
                    url.ValueKind != JsonValueKind.Null);
        }
        client.DefaultRequestHeaders.Remove(CorrelationIdMiddleware.HeaderName);

        using (var scope = fixture.Host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var audit = Assert.Single(await db.Set<RegistroAuditoria>().ToListAsync());
            Assert.Null(audit.EmpresaId);
            Assert.Equal(AmbitoAutorizacion.Plataforma, audit.AmbitoAutoridad);
            Assert.NotNull(audit.ValoresNuevos);
            using var metadata = JsonDocument.Parse(audit.ValoresNuevos!);
            Assert.Equal("qa-check-evidence/v1", metadata.RootElement.GetProperty("Schema").GetString());
            Assert.Equal(Sha, metadata.RootElement.GetProperty("Sha").GetString());
            Assert.Equal("CI_MINIMA_ACREDITADA", metadata.RootElement.GetProperty("EstadoCi").GetString());
            Assert.Equal("SIN_VERIFICAR", metadata.RootElement.GetProperty("EstadoA5").GetString());
            Assert.Equal(4, metadata.RootElement.GetProperty("TotalChecks").GetInt32());
            Assert.Equal(4, metadata.RootElement.GetProperty("ChecksExitosos").GetInt32());
            Assert.DoesNotContain("private-secret", audit.ValoresNuevos!);
            Assert.Equal(goodCorrelation, audit.CorrelationId);
        }

        // Real HTTP + JWT + SQL: an invalid upstream contract must never yield a
        // 500 or evidence, and its bounded audit must survive a fresh context.
        upstream.Payload = "[\"private-secret\"]";
        client.DefaultRequestHeaders.Add(CorrelationIdMiddleware.HeaderName, "invalid correlation value");
        string errorCorrelation;
        using (var malformed = await client.GetAsync(Path))
        {
            Assert.Equal(HttpStatusCode.ServiceUnavailable, malformed.StatusCode);
            errorCorrelation = Assert.Single(malformed.Headers.GetValues(CorrelationIdMiddleware.HeaderName));
            Assert.True(Guid.TryParseExact(errorCorrelation, "N", out _));
            Assert.Contains("no-store", malformed.Headers.CacheControl?.ToString() ?? "");
            Assert.DoesNotContain("private-secret", await malformed.Content.ReadAsStringAsync());
        }
        client.DefaultRequestHeaders.Remove(CorrelationIdMiddleware.HeaderName);
        using (var scope = fixture.Host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var entries = await db.Set<RegistroAuditoria>().ToListAsync();
            Assert.Equal(2, entries.Count);
            var failure = Assert.Single(entries, a => a.Resultado == "Error");
            Assert.Equal("QA_UPSTREAM_CONTRACT", failure.Error);
            Assert.Null(failure.EmpresaId);
            Assert.Equal(AmbitoAutorizacion.Plataforma, failure.AmbitoAutoridad);
            Assert.DoesNotContain("private-secret", failure.ValoresNuevos!);
            using var metadata = JsonDocument.Parse(failure.ValoresNuevos!);
            Assert.Equal(Sha, metadata.RootElement.GetProperty("Sha").GetString());
            Assert.Equal("SIN_VERIFICAR", metadata.RootElement.GetProperty("EstadoCi").GetString());
            Assert.Equal(0, metadata.RootElement.GetProperty("ChecksExitosos").GetInt32());
            Assert.Equal(errorCorrelation, failure.CorrelationId);
        }

        // Real storage failure, not a mocked DbUpdateException: block INSERT
        // in this fixture's unique local MySQL schema and verify both paths
        // fail closed without disclosing provider evidence or SQL diagnostics.
        using (var scope = fixture.Host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.ExecuteSqlRawAsync(
                "CREATE TRIGGER qa_ci_audit_insert_failure BEFORE INSERT ON RegistrosAuditoria " +
                "FOR EACH ROW SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'qa-ci-audit-write-blocked'");
        }
        try
        {
            foreach (var upstreamPayload in new[] { Payload(), "[\"private-secret\"]" })
            {
                upstream.Payload = upstreamPayload;
                using var blockedAudit = await client.GetAsync(Path);
                Assert.Equal(HttpStatusCode.ServiceUnavailable, blockedAudit.StatusCode);
                Assert.Contains("no-store", blockedAudit.Headers.CacheControl?.ToString() ?? "");
                var body = await blockedAudit.Content.ReadAsStringAsync();
                Assert.DoesNotContain("CI_MINIMA_ACREDITADA", body);
                Assert.DoesNotContain("qa-ci-audit-write-blocked", body);
                Assert.DoesNotContain("private-secret", body);
            }
            using var scope = fixture.Host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.Equal(2, await db.Set<RegistroAuditoria>().CountAsync());
        }
        finally
        {
            using var scope = fixture.Host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.ExecuteSqlRawAsync("DROP TRIGGER IF EXISTS qa_ci_audit_insert_failure");
        }

        // Recover the real SQL sink; no failed insert may survive the fault.
        upstream.Payload = Payload();
        using (var recovered = await client.GetAsync(Path))
            Assert.Equal(HttpStatusCode.OK, recovered.StatusCode);
        using (var scope = fixture.Host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.Equal(3, await db.Set<RegistroAuditoria>().CountAsync());
            Assert.Equal(2, await db.Set<RegistroAuditoria>().CountAsync(a => a.Resultado == "Exito"));
        }

        // Revoke after the initial authorization, while the provider request is
        // in flight. The response must never disclose a successful QA result.
        upstream.Payload = Payload();
        upstream.BeforeResponse = async () =>
        {
            using var scope = fixture.Host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var grant = await db.Set<UsuarioRolPlataforma>().SingleAsync(
                x => x.UsuarioId == fixture.GlobalId);
            grant.Desactivar();
            await db.SaveChangesAsync();
        };
        using (var revokedInFlight = await client.GetAsync(Path))
        {
            Assert.Equal(HttpStatusCode.Forbidden, revokedInFlight.StatusCode);
            Assert.DoesNotContain("CI_MINIMA_ACREDITADA", await revokedInFlight.Content.ReadAsStringAsync());
        }
        using (var scope = fixture.Host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var entries = await db.Set<RegistroAuditoria>().ToListAsync();
            Assert.Equal(4, entries.Count);
            Assert.Equal(2, entries.Count(a => a.Resultado == "Exito"));
            var revoked = Assert.Single(entries, a => a.Error == "QA_ACCESS_DENIED");
            AssertDenialAudit(revoked, fixture.GlobalId);
        }
        using (var denied = await client.GetAsync(Path))
            Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        Assert.Equal(6, upstream.Calls);
        using (var scope = fixture.Host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var entries = await db.Set<RegistroAuditoria>().ToListAsync();
            Assert.Equal(5, entries.Count);
            Assert.Equal(2, entries.Count(a => a.Resultado == "Exito"));
            var denials = entries.Where(a => a.Error == "QA_ACCESS_DENIED").ToList();
            Assert.Equal(2, denials.Count);
            Assert.All(denials, a => AssertDenialAudit(a, fixture.GlobalId));
        }
    }

    // This test runs against an isolated MySQL 8.4 service in the GH-hosted QA lab.
    // Outside that CI lane the MySQL branch does not count as real integration.
    [Fact]
    public async Task LaboratorioMysql84JwtTenantsAyBDeniegaAccesoGlobalYSelectorCruzado()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(
                "SOLQARYN_C0204_MYSQL_TEST_CONNECTION")))
            return;

        var upstream = new GitHubFake(Payload());
        await using var fixture = await CrearAsync(upstream, mysql: true);
        int actorA, actorB, tenantA, tenantB;
        using (var scope = fixture.Host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var roleId = await db.Roles
                .Where(r => r.NombreNormalizado == "ADMIN_EMP_QA")
                .Select(r => r.Id).SingleAsync();
            var companyA = new Empresa("QA ephemeral tenant A");
            var companyB = new Empresa("QA ephemeral tenant B");
            db.Set<Empresa>().AddRange(companyA, companyB);
            await db.SaveChangesAsync();

            var userA = new Usuario
            {
                NombreUsuario = "qa_company_a", NombreCompleto = "QA company A",
                PasswordHash = "ci-no-password", RolId = roleId, Activo = true
            };
            var userB = new Usuario
            {
                NombreUsuario = "qa_company_b", NombreCompleto = "QA company B",
                PasswordHash = "ci-no-password", RolId = roleId, Activo = true
            };
            db.Usuarios.AddRange(userA, userB);
            await db.SaveChangesAsync();
            db.Set<UsuarioEmpresa>().AddRange(
                new UsuarioEmpresa(userA.Id, companyA.Id, roleId),
                new UsuarioEmpresa(userB.Id, companyB.Id, roleId));
            await db.SaveChangesAsync();

            actorA = userA.Id;
            actorB = userB.Id;
            tenantA = companyA.Id;
            tenantB = companyB.Id;
            Assert.NotEqual(tenantA, tenantB);
            Assert.Equal(2, await db.Set<UsuarioEmpresa>()
                .CountAsync(m => m.Activa &&
                    (m.UsuarioId == actorA || m.UsuarioId == actorB)));
        }

        using var client = fixture.Host.GetTestClient();
        foreach (var (actor, selector) in new[]
        {
            (actorA, tenantA),
            (actorA, tenantB),
            (actorB, tenantB),
            (actorB, tenantA)
        })
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Bearer", Token(actor, "company-ci-" + actor, "true"));
            client.DefaultRequestHeaders.Remove("X-Empresa-Id");
            client.DefaultRequestHeaders.Add("X-Empresa-Id", selector.ToString());
            using var response = await client.GetAsync(Path);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.DoesNotContain("private-secret", body);
            Assert.DoesNotContain("QA ephemeral tenant", body);
        }
        Assert.Equal(0, upstream.Calls);

        using (var scope = fixture.Host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var entries = await db.Set<RegistroAuditoria>().ToListAsync();
            Assert.Equal(4, entries.Count);
            Assert.Equal(2, entries.Count(a => a.UsuarioId == actorA));
            Assert.Equal(2, entries.Count(a => a.UsuarioId == actorB));
            Assert.All(entries, a => AssertDenialAudit(a, a.UsuarioId == actorA ? actorA : actorB));
            await db.Database.ExecuteSqlRawAsync(
                "CREATE TRIGGER qa_ci_denial_insert_failure BEFORE INSERT ON RegistrosAuditoria " +
                "FOR EACH ROW SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'qa-ci-denial-write-blocked'");
        }
        try
        {
            using var denied = await client.GetAsync(Path);
            Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
            Assert.Contains("no-store", denied.Headers.CacheControl?.ToString() ?? "");
            Assert.DoesNotContain("qa-ci-denial-write-blocked", await denied.Content.ReadAsStringAsync());
            Assert.Equal(0, upstream.Calls);
            using var scope = fixture.Host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.Equal(4, await db.Set<RegistroAuditoria>().CountAsync());
        }
        finally
        {
            using var scope = fixture.Host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.ExecuteSqlRawAsync("DROP TRIGGER IF EXISTS qa_ci_denial_insert_failure");
        }
    }

    private static void AssertDenialAudit(RegistroAuditoria row, int actorId)
    {
        Assert.Equal(actorId, row.UsuarioId);
        Assert.Equal("actor-autenticado", row.NombreUsuario);
        Assert.Null(row.EmpresaId);
        Assert.Null(row.AmbitoAutoridad);
        Assert.Null(row.OrigenAutorizacion);
        Assert.Equal("Error", row.Resultado);
        Assert.Equal("QA_ACCESS_DENIED", row.Error);
        Assert.Equal("DenegacionEvidenciasQaGlobal", row.Entidad);
        Assert.NotNull(row.CorrelationId);
        Assert.Null(row.Ip);
        Assert.Null(row.UserAgent);
        using var metadata = JsonDocument.Parse(row.ValoresNuevos!);
        var root = metadata.RootElement;
        Assert.Equal(3, root.EnumerateObject().Count());
        Assert.Equal("qa-access-denial/v1", root.GetProperty("Schema").GetString());
        Assert.Equal("Plataforma", root.GetProperty("TargetScope").GetString());
        Assert.False(root.GetProperty("AuthorityGranted").GetBoolean());
    }

    private static string Token(int id, string username, string admin = "false")
    {
        var jwt = new JwtSecurityToken(
            issuer: Issuer, audience: Audience,
            claims:
            [
                new Claim("id", id.ToString()),
                new Claim("nombreUsuario", username),
                new Claim("esAdministrador", admin)
            ],
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: new SigningCredentials(TestKey, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }


    [Fact]
    public async Task LaboratorioMysql84RechazaConclusionesExternasInvalidasSinEco()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(
                "SOLQARYN_C0204_MYSQL_TEST_CONNECTION")))
            return;
        var upstream = new GitHubFake(Payload());
        await using var fixture = await CrearAsync(upstream, mysql: true);
        using var client = fixture.Host.GetTestClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", Token(fixture.GlobalId, "operadorglobal"));

        foreach (var invalid in new[]
        {
            Payload("private-secret"),
            Payload().Replace("\"status\":\"completed\"", "\"status\":\"queued\""),
            Payload().Replace("\"conclusion\":\"success\"", "\"conclusion\":null")
        })
        {
            upstream.Payload = invalid;
            using var denied = await client.GetAsync(Path);
            Assert.Equal(HttpStatusCode.ServiceUnavailable, denied.StatusCode);
            Assert.DoesNotContain("private-secret", await denied.Content.ReadAsStringAsync());
        }

        // A legitimate queued check with a null conclusion is not a PASS.
        upstream.Payload = Payload().Replace("\"status\":\"completed\"", "\"status\":\"queued\"")
            .Replace("\"conclusion\":\"success\"", "\"conclusion\":null");
        using (var queued = await client.GetAsync(Path))
        {
            Assert.Equal(HttpStatusCode.OK, queued.StatusCode);
            using var json = JsonDocument.Parse(await queued.Content.ReadAsStringAsync());
            Assert.Equal("CI_EN_PROGRESO", json.RootElement.GetProperty("data").GetProperty("estadoCi").GetString());
            Assert.Equal(0, json.RootElement.GetProperty("data").GetProperty("checksExitosos").GetInt32());
        }

        using var scope = fixture.Host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var audits = await db.Set<RegistroAuditoria>().ToListAsync();
        Assert.Equal(4, audits.Count);
        Assert.Equal(3, audits.Count(x => x.Resultado == "Error" && x.Error == "QA_UPSTREAM_CONTRACT"));
        Assert.All(audits, x => Assert.DoesNotContain("private-secret", x.ValoresNuevos ?? ""));
    }

    private static string Payload(string conclusion = "success", string[]? detailsUrls = null)
    {
        var checkIndex = 0;
        object Check(string name, string result) => new
        {
            head_sha = Sha,
            name,
            status = "completed",
            conclusion = result,
            details_url = detailsUrls is null ? "https://github.com/solqaryn/Solqaryn/actions/runs/123/job/456" : detailsUrls[checkIndex++],
            output = new { summary = "private-secret-not-for-the-response" }
        };
        return JsonSerializer.Serialize(new
        {
            total_count = 4,
            check_runs = new[]
            {
                Check("verify-solqaryn-project-scope", "success"),
                Check("V5 - Sin encabezados vacios ni atribucion cruzada", "success"),
                Check("SOLQARYN - Seguridad multiempresa", "success"),
                Check("Backend Release y pruebas", conclusion)
            }
        });
    }

    private static async Task<Fixture> CrearAsync(GitHubFake upstream, bool mysql = false)
    {
        string? connection = null;
        if (mysql)
        {
            var raw = Environment.GetEnvironmentVariable("SOLQARYN_C0204_MYSQL_TEST_CONNECTION")
                ?? throw new InvalidOperationException("MySQL local requerido.");
            var conf = new MySqlConnectionStringBuilder(raw);
            if (conf.Server is not ("127.0.0.1" or "localhost"))
                throw new InvalidOperationException("MySQL de prueba debe ser local descartable.");
            conf.Database = "solqaryn_c0204_qa_" + Guid.NewGuid().ToString("N");
            connection = conf.ConnectionString;
        }

        var dbName = "c0204-qa-http-" + Guid.NewGuid().ToString("N");
        var dbConnection = connection;
        var host = await new HostBuilder()
            .ConfigureWebHost(web => web.UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddLogging();
                    services.AddHttpContextAccessor();
                    services.AddDbContext<AppDbContext>(opts =>
                    {
                        if (dbConnection is not null)
                            opts.UseMySQL(dbConnection, mysqlOptions =>
                                mysqlOptions.MigrationsAssembly("Solqaryn.Infrastructure.Migrations"));
                        else
                            opts.UseInMemoryDatabase(dbName);
                    });
                    services.AddScoped<ICurrentUserService, CurrentUserService>();
                    services.AddScoped<IPlataformaScopeService, PlataformaScopeService>();
                    services.AddHttpClient("SolqarynPublicGitHubQa",
                            c => c.BaseAddress = new Uri("https://api.github.com/"))
                        .ConfigurePrimaryHttpMessageHandler(() => upstream);
                    services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                        .AddJwtBearer(options => options.TokenValidationParameters =
                            new TokenValidationParameters
                            {
                                ValidateIssuer = true,
                                ValidateAudience = true,
                                ValidateLifetime = true,
                                ValidateIssuerSigningKey = true,
                                ValidIssuer = Issuer,
                                ValidAudience = Audience,
                                IssuerSigningKey = TestKey,
                                ClockSkew = TimeSpan.Zero
                            });
                    services.AddAuthorization();
                    services.AddControllers().AddApplicationPart(
                        typeof(PlataformaQaEvidenciasController).Assembly);
                })
                .Configure(app =>
                {
                    app.UseMiddleware<CorrelationIdMiddleware>();
                    app.UseRouting();
                    app.UseAuthentication();
                    app.UseAuthorization();
                    app.UseEndpoints(endpoints => endpoints.MapControllers());
                })).StartAsync();

        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            if (dbConnection is not null)
                await Phase7MySqlTestDatabase.InitializeFreshAsync(db);
            var company = new Rol
            {
                Nombre = "Admin empresa", NombreNormalizado = "ADMIN_EMP_QA",
                EsSistema = true, EsAdministrador = true, Activo = true,
                Ambito = AmbitoAutorizacion.Empresa
            };
            var global = new Rol
            {
                Nombre = "Admin SOLQARYN", NombreNormalizado = "GLOBAL_PLAT_QA",
                EsSistema = true, EsAdministrador = true, Activo = true,
                Ambito = AmbitoAutorizacion.Plataforma
            };
            db.Roles.AddRange(company, global);
            await db.SaveChangesAsync();
            var globalUser = new Usuario
            {
                NombreUsuario = "operadorglobal", NombreCompleto = "Operador global",
                PasswordHash = "no-password", RolId = company.Id, Activo = true
            };
            var adminEmpresa = new Usuario
            {
                NombreUsuario = "adminempresa", NombreCompleto = "Admin empresa",
                PasswordHash = "no-password", RolId = company.Id, Activo = true
            };
            db.Usuarios.AddRange(globalUser, adminEmpresa);
            await db.SaveChangesAsync();
            db.Set<UsuarioRolPlataforma>().Add(
                new UsuarioRolPlataforma(globalUser.Id, global.Id));
            await db.SaveChangesAsync();
            return new Fixture(host, globalUser.Id, adminEmpresa.Id, dbConnection);
        }
    }

    private sealed class Fixture(
        IHost host, int globalId, int empresaAdminId, string? mysqlConnection)
        : IAsyncDisposable
    {
        public IHost Host { get; } = host;
        public int GlobalId { get; } = globalId;
        public int EmpresaAdminId { get; } = empresaAdminId;

        public async ValueTask DisposeAsync()
        {
            await Host.StopAsync();
            Host.Dispose();
            if (mysqlConnection is not null)
            {
                var options = new DbContextOptionsBuilder<AppDbContext>()
                    .UseMySQL(mysqlConnection, m =>
                        m.MigrationsAssembly("Solqaryn.Infrastructure.Migrations")).Options;
                await using var db = new AppDbContext(options);
                await db.Database.EnsureDeletedAsync();
            }
        }
    }

    private sealed class GitHubFake(string payload) : HttpMessageHandler
    {
        public int Calls { get; private set; }
        public string? LastHost { get; private set; }
        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;
        public string Payload { get; set; } = payload;
        public bool OmitContentLength { get; set; }
        public Func<Task>? BeforeResponse { get; set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            LastHost = request.RequestUri?.Host;
            if (LastHost != "api.github.com" ||
                request.RequestUri?.AbsolutePath !=
                "/repos/solqaryn/Solqaryn/commits/" + Sha + "/check-runs")
                throw new InvalidOperationException("Egreso fuera del repositorio canónico.");
            if (BeforeResponse is not null)
                await BeforeResponse();
            return new HttpResponseMessage(StatusCode)
            {
                Content = OmitContentLength
                    ? new ChunkedQaContent(Payload)
                    : new StringContent(Payload, Encoding.UTF8, "application/json")
            };
        }
    }

    // Forces the upstream Content-Length to be absent, just like a genuine
    // chunked GitHub API response. TestServer exercises the real HTTP client.
    private sealed class ChunkedQaContent(string payload) : HttpContent
    {
        protected override bool TryComputeLength(out long length)
        {
            length = -1;
            return false;
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            var bytes = Encoding.UTF8.GetBytes(payload);
            return stream.WriteAsync(bytes, 0, bytes.Length);
        }
    }
}

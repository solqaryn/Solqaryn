using System.Data.Common;
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
using Microsoft.EntityFrameworkCore.Diagnostics;
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
        Assert.Equal(4, upstream.Calls);
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
        var identities = metadata.RootElement.GetProperty("ChecksIdentificados");
        Assert.Equal(new long[] { 456, 457, 458, 459 },
            identities.EnumerateArray().Select(x => x.GetProperty("CheckId").GetInt64()).ToArray());
        Assert.All(identities.EnumerateArray(), x =>
        {
            Assert.Equal(15368, x.GetProperty("AppId").GetInt64());
            Assert.Equal("github-actions", x.GetProperty("AppSlug").GetString());
            Assert.True(x.GetProperty("CheckSuiteId").GetInt64() > 0);
        });
        Assert.DoesNotContain("private-secret", audit.ValoresNuevos!);
        Assert.True(Guid.TryParseExact(audit.CorrelationId, "N", out _));
        await AssertRevocationDuringSecondaryReadAsync(upstream);
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
        Assert.Equal(4, upstream.Calls);
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

        Assert.Equal(5, upstream.Calls);
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
            "https://github.com/solqaryn/Solqaryn/actions/runs/124/job/7900"
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
            for (var i = 1; i < 3; i++)
                Assert.False(checks[i].TryGetProperty("urlEvidencia", out var url) &&
                    url.ValueKind != JsonValueKind.Null);
            Assert.Equal("https://github.com/solqaryn/Solqaryn/actions/runs/124/job/7900",
                checks[3].GetProperty("urlEvidencia").GetString());
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
        Assert.Equal(15, upstream.Calls);
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

        // Force revocation at the actual audit INSERT, not during upstream I/O.
        // This fixture owns a disposable local MySQL schema.
        upstream.BeforeResponse = null;
        using (var scope = fixture.Host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.ExecuteSqlRawAsync(
                "CREATE TRIGGER qa_ci_revoke_on_audit AFTER INSERT ON RegistrosAuditoria " +
                "FOR EACH ROW UPDATE UsuarioRolesPlataforma SET Activa = 0 " +
                "WHERE UsuarioId = NEW.UsuarioId AND NEW.Entidad = 'ConsultaEvidenciasQaGlobal'");
        }
        try
        {
            // The client may reuse a syntactically valid correlation value.
            // Link the compensation to the exact persisted audit, not that value.
            var correlation = Guid.NewGuid().ToString("N");
            var previousAuditId = 0;
            foreach (var (payload, expectedAuditResult) in new[]
            {
                (Payload(), "Exito"),
                ("[\"private-secret\"]", "Error")
            })
            {
                using (var scope = fixture.Host.Services.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var grant = await db.Set<UsuarioRolPlataforma>().SingleAsync(
                        x => x.UsuarioId == fixture.GlobalId);
                    grant.Activar();
                    await db.SaveChangesAsync();
                }
                upstream.Payload = payload;
                using var request = new HttpRequestMessage(HttpMethod.Get, Path);
                request.Headers.Add(CorrelationIdMiddleware.HeaderName, correlation);
                using var response = await client.SendAsync(request);
                Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
                Assert.Contains("no-store", response.Headers.CacheControl?.ToString() ?? "");
                var body = await response.Content.ReadAsStringAsync();
                Assert.DoesNotContain("CI_MINIMA_ACREDITADA", body);
                Assert.DoesNotContain("private-secret", body);

                using var scopeAfter = fixture.Host.Services.CreateScope();
                var after = scopeAfter.ServiceProvider.GetRequiredService<AppDbContext>();
                var rows = await after.Set<RegistroAuditoria>().Where(
                    x => x.CorrelationId == correlation && x.Id > previousAuditId).ToListAsync();
                Assert.Equal(2, rows.Count);
                previousAuditId = rows.Max(x => x.Id);
                var query = Assert.Single(rows, x => x.Entidad == "ConsultaEvidenciasQaGlobal");
                Assert.Equal(expectedAuditResult, query.Resultado);
                var deniedAfterWrite = Assert.Single(rows, x => x.Error == "QA_ACCESS_DENIED");
                AssertDenialAudit(deniedAfterWrite, fixture.GlobalId, query.Id);
                Assert.False((await after.Set<UsuarioRolPlataforma>().SingleAsync(
                    x => x.UsuarioId == fixture.GlobalId)).Activa);
            }

            foreach (var mutatePriorAuditId in new[] { true, false })
            {
                // The prior receipt is already durable; a silently mutated
                // compensation must not invent its causal link or timestamp.
                using (var scope = fixture.Host.Services.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var grant = await db.Set<UsuarioRolPlataforma>().SingleAsync(
                        x => x.UsuarioId == fixture.GlobalId);
                    grant.Activar();
                    await db.SaveChangesAsync();
                    // Both DDL commands are fixed test literals, never assembled from SQL fragments.
                    if (mutatePriorAuditId)
                        await db.Database.ExecuteSqlRawAsync(
                            "CREATE TRIGGER qa_ci_mutate_compensation BEFORE INSERT ON RegistrosAuditoria FOR EACH ROW SET NEW.ValoresNuevos = IF(NEW.Entidad = 'DenegacionEvidenciasQaGlobal', JSON_SET(NEW.ValoresNuevos, '$.PriorAuditId', 0), NEW.ValoresNuevos)");
                    else
                        await db.Database.ExecuteSqlRawAsync(
                            "CREATE TRIGGER qa_ci_mutate_compensation BEFORE INSERT ON RegistrosAuditoria FOR EACH ROW SET NEW.Fecha = IF(NEW.Entidad = 'DenegacionEvidenciasQaGlobal', DATE_ADD(NEW.Fecha, INTERVAL 1 DAY), NEW.Fecha)");
                }
                try
                {
                    var mutatedCorrelation = Guid.NewGuid().ToString("N");
                    upstream.Payload = Payload();
                    using var request = new HttpRequestMessage(HttpMethod.Get, Path);
                    request.Headers.Add(CorrelationIdMiddleware.HeaderName, mutatedCorrelation);
                    using var response = await client.SendAsync(request);
                    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
                    Assert.Contains("no-store", response.Headers.CacheControl?.ToString() ?? "");
                    var body = await response.Content.ReadAsStringAsync();
                    Assert.DoesNotContain("CI_MINIMA_ACREDITADA", body);
                    Assert.DoesNotContain("PriorAuditId", body);
                    Assert.DoesNotContain("qa_ci_mutate_compensation", body);

                    using var scopeAfter = fixture.Host.Services.CreateScope();
                    var after = scopeAfter.ServiceProvider.GetRequiredService<AppDbContext>();
                    var prior = Assert.Single(await after.Set<RegistroAuditoria>().Where(
                        x => x.CorrelationId == mutatedCorrelation).ToListAsync());
                    Assert.Equal("ConsultaEvidenciasQaGlobal", prior.Entidad);
                    Assert.Equal("Exito", prior.Resultado);
                    Assert.False((await after.Set<UsuarioRolPlataforma>().SingleAsync(
                        x => x.UsuarioId == fixture.GlobalId)).Activa);
                }
                finally
                {
                    using var scope = fixture.Host.Services.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    await db.Database.ExecuteSqlRawAsync("DROP TRIGGER IF EXISTS qa_ci_mutate_compensation");
                }
            }
        }
        finally
        {
            using var scope = fixture.Host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.ExecuteSqlRawAsync("DROP TRIGGER IF EXISTS qa_ci_revoke_on_audit");
        }

        // Force an actual failed authority SELECT in the disposable MySQL
        // schema, both before provider I/O and during the upstream request.
        int auditCount;
        using (var scope = fixture.Host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var grant = await db.Set<UsuarioRolPlataforma>().SingleAsync(
                x => x.UsuarioId == fixture.GlobalId);
            grant.Activar();
            await db.SaveChangesAsync();
            auditCount = await db.Set<RegistroAuditoria>().CountAsync();
        }

        async Task MoveAuthorityTableAsync(bool unavailable)
        {
            using var scope = fixture.Host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.ExecuteSqlRawAsync(unavailable
                ? "RENAME TABLE UsuarioRolesPlataforma TO qa_ci_authority_unavailable"
                : "RENAME TABLE qa_ci_authority_unavailable TO UsuarioRolesPlataforma");
        }

        async Task AssertAuthorityUnavailableAsync()
        {
            using var response = await client.GetAsync(Path);
            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            Assert.Contains("no-store", response.Headers.CacheControl?.ToString() ?? "");
            var body = await response.Content.ReadAsStringAsync();
            Assert.DoesNotContain("CI_MINIMA_ACREDITADA", body);
            Assert.DoesNotContain("UsuarioRolesPlataforma", body);
            Assert.DoesNotContain("qa_ci_authority_unavailable", body);
            Assert.DoesNotContain("private-secret", body);
        }

        var callsBeforeUnavailable = upstream.Calls;
        await MoveAuthorityTableAsync(true);
        try
        {
            await AssertAuthorityUnavailableAsync();
            Assert.Equal(callsBeforeUnavailable, upstream.Calls);
        }
        finally
        {
            await MoveAuthorityTableAsync(false);
        }

        foreach (var unavailablePayload in new[] { Payload(), "[\"private-secret\"]" })
        {
            upstream.Payload = unavailablePayload;
            var moved = false;
            upstream.BeforeResponse = async () =>
            {
                upstream.BeforeResponse = null;
                await MoveAuthorityTableAsync(true);
                moved = true;
            };
            try
            {
                await AssertAuthorityUnavailableAsync();
                Assert.True(moved);
            }
            finally
            {
                upstream.BeforeResponse = null;
                if (moved) await MoveAuthorityTableAsync(false);
            }
        }

        using (var scope = fixture.Host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.Equal(auditCount, await db.Set<RegistroAuditoria>().CountAsync());
        }
        upstream.Payload = Payload();
        using (var restored = await client.GetAsync(Path))
            Assert.Equal(HttpStatusCode.OK, restored.StatusCode);
        using (var scope = fixture.Host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.Equal(auditCount + 1, await db.Set<RegistroAuditoria>().CountAsync());
        }

        // An apparently successful INSERT is insufficient: a real SQL trigger
        // can alter the audit receipt. Both success and error must roll back.
        foreach (var triggerSql in new[]
        {
            "CREATE TRIGGER qa_ci_audit_readback_mutation BEFORE INSERT ON RegistrosAuditoria FOR EACH ROW SET NEW.CorrelationId = 'qa-ci-mutated'",
            "CREATE TRIGGER qa_ci_audit_readback_mutation BEFORE INSERT ON RegistrosAuditoria FOR EACH ROW SET NEW.ValoresNuevos = JSON_SET(NEW.ValoresNuevos, '$.EstadoCi', 'qa-ci-mutated')",
            "CREATE TRIGGER qa_ci_audit_readback_mutation BEFORE INSERT ON RegistrosAuditoria FOR EACH ROW SET NEW.Fecha = DATE_ADD(NEW.Fecha, INTERVAL 1 DAY)"
        })
        {
            using (var scope = fixture.Host.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await db.Database.ExecuteSqlRawAsync(triggerSql);
            }
            try
            {
                foreach (var payload in new[] { Payload(), "[\"private-secret\"]" })
                {
                    upstream.Payload = payload;
                    using var rejected = await client.GetAsync(Path);
                    Assert.Equal(HttpStatusCode.ServiceUnavailable, rejected.StatusCode);
                    var body = await rejected.Content.ReadAsStringAsync();
                    Assert.DoesNotContain("CI_MINIMA_ACREDITADA", body);
                    Assert.DoesNotContain("qa-ci-mutated", body);
                    Assert.DoesNotContain("private-secret", body);
                    using var scope = fixture.Host.Services.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    Assert.Equal(auditCount + 1, await db.Set<RegistroAuditoria>().CountAsync());
                }
            }
            finally
            {
                using var scope = fixture.Host.Services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await db.Database.ExecuteSqlRawAsync("DROP TRIGGER IF EXISTS qa_ci_audit_readback_mutation");
            }
        }
        upstream.Payload = Payload();
        using (var recovered = await client.GetAsync(Path))
            Assert.Equal(HttpStatusCode.OK, recovered.StatusCode);
        using (var scope = fixture.Host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.Equal(auditCount + 2, await db.Set<RegistroAuditoria>().CountAsync());
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
        var denialReadbackFault = new DenialReadbackFault();
        await using var fixture = await CrearAsync(upstream, mysql: true, denialReadbackFault: denialReadbackFault);
        // The signed JWT has a valid global grant in SQL, but ambiguous or
        // noncanonical identity must not be normalized into an audit actor.
        using (var ambiguous = fixture.Host.GetTestClient())
        {
            var canonical = fixture.GlobalId.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var malformed = new[]
            {
                Token(fixture.GlobalId, "operadorglobal", actorId: "0" + canonical),
                Token(fixture.GlobalId, "operadorglobal", actorId: "+" + canonical),
                Token(fixture.GlobalId, "operadorglobal", actorId: " " + canonical),
                Token(fixture.GlobalId, "operadorglobal", duplicateActorId: canonical),
                Token(fixture.GlobalId, "operadorglobal", duplicateUsername: "operadorglobal")
            };
            foreach (var jwt in malformed)
            {
                ambiguous.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
                using var response = await ambiguous.GetAsync(Path);
                Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
                Assert.Contains("no-store", response.Headers.CacheControl?.ToString() ?? "");
            }
            Assert.Equal(0, upstream.Calls);
            using var scope = fixture.Host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.Empty(await db.Set<RegistroAuditoria>().ToListAsync());
        }
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

        // Fail the actual Oracle MySQL SELECT after INSERT; a known denial
        // must remain 403 and the uncommitted receipt must roll back.
        foreach (var (actor, selector) in new[] { (actorA, tenantB), (actorB, tenantA) })
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Bearer", Token(actor, "company-ci-" + actor, "true"));
            client.DefaultRequestHeaders.Remove("X-Empresa-Id");
            client.DefaultRequestHeaders.Add("X-Empresa-Id", selector.ToString());
            denialReadbackFault.Enabled = true;
            try
            {
                using var denied = await client.GetAsync(Path);
                Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
                Assert.Contains("no-store", denied.Headers.CacheControl?.ToString() ?? "");
                Assert.DoesNotContain("qa_ci_missing_denial_receipt_table",
                    await denied.Content.ReadAsStringAsync());
                Assert.Equal(0, upstream.Calls);
            }
            finally
            {
                denialReadbackFault.Enabled = false;
            }
            using var scope = fixture.Host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.Equal(4, await db.Set<RegistroAuditoria>().CountAsync());
        }
        Assert.Equal(2, denialReadbackFault.Failures);
    }

    private static void AssertDenialAudit(RegistroAuditoria row, int actorId, int? priorAuditId = null)
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
        Assert.Equal(priorAuditId.HasValue ? 4 : 3, root.EnumerateObject().Count());
        if (priorAuditId.HasValue)
        {
            Assert.True(priorAuditId.Value > 0);
            Assert.Equal(priorAuditId.Value, root.GetProperty("PriorAuditId").GetInt32());
        }
        else
            Assert.False(root.TryGetProperty("PriorAuditId", out _));
        Assert.Equal("qa-access-denial/v1", root.GetProperty("Schema").GetString());
        Assert.Equal("Plataforma", root.GetProperty("TargetScope").GetString());
        Assert.False(root.GetProperty("AuthorityGranted").GetBoolean());
    }

    private static string Token(int id, string username, string admin = "false",
        string? actorId = null, string? duplicateActorId = null, string? duplicateUsername = null)
    {
        var claims = new List<Claim>
        {
            new("id", actorId ?? id.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            new("nombreUsuario", username),
            new("esAdministrador", admin)
        };
        if (duplicateActorId is not null) claims.Add(new Claim("id", duplicateActorId));
        if (duplicateUsername is not null) claims.Add(new Claim("nombreUsuario", duplicateUsername));
        var jwt = new JwtSecurityToken(
            issuer: Issuer, audience: Audience,
            claims: claims,
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
        var upstream = new GitHubFake(Payload()) { ProviderTimeout = TimeSpan.FromSeconds(1) };
        await using var fixture = await CrearAsync(upstream, mysql: true);
        using var client = fixture.Host.GetTestClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", Token(fixture.GlobalId, "operadorglobal"));

        foreach (var invalid in new[]
        {
            Payload("private-secret"),
            Payload().Replace("\"status\":\"completed\"", "\"status\":\"queued\""),
            Payload().Replace("\"conclusion\":\"success\"", "\"conclusion\":null"),
            // Last-value-wins parsing used to accept these contradictory fields.
            Payload().Replace("\"total_count\":4", "\"total_count\":1,\"total_count\":4"),
            Payload().Replace("\"conclusion\":\"success\"", "\"conclusion\":\"failure\",\"conclusion\":\"success\""),
            Payload().Replace("\"head_sha\":", "\"head_sha\":\"private-secret\",\"head_sha\":"),
            // Required evidence must retain a unique, positive check and suite identity
            // from GitHub Actions; matching names and SHA alone are insufficient.
            Payload().Replace("\"id\":456,", ""),
            Payload().Replace("\"id\":457,", "\"id\":456,"),
            Payload().Replace("\"id\":456,", "\"id\":0,"),
            Payload().Replace("\"check_suite\":{\"id\":789}", "\"check_suite\":{\"id\":0}"),
            Payload().Replace("\"slug\":\"github-actions\"", "\"slug\":\"private-secret\""),
            Payload().Replace("\"id\":15368,", "\"id\":15369,")
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

        // Headers arrive, but the body never yields bytes until cancellation.
        // A separate test deadline prevents the unfixed implementation from hanging CI.
        var stalled = new StalledQaBody();
        upstream.StalledBody = stalled;
        using (var testDeadline = new CancellationTokenSource(TimeSpan.FromSeconds(12)))
        using (var timedOut = await client.GetAsync(Path, testDeadline.Token))
        {
            Assert.Equal(HttpStatusCode.ServiceUnavailable, timedOut.StatusCode);
            Assert.True(stalled.ReadStarted);
            Assert.Contains("no-store", timedOut.Headers.CacheControl?.ToString() ?? "");
            Assert.DoesNotContain("private-secret", await timedOut.Content.ReadAsStringAsync());
        }

        upstream.StalledBody = null;
        upstream.Payload = Payload();
        using (var recovered = await client.GetAsync(Path))
            Assert.Equal(HttpStatusCode.OK, recovered.StatusCode);

        using var scope = fixture.Host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var audits = await db.Set<RegistroAuditoria>().ToListAsync();
        Assert.Equal(15, audits.Count);
        Assert.Single(audits.Where(x => x.Resultado == "Error" && x.Error == "QA_UPSTREAM_TIMEOUT"));
        Assert.Equal(12, audits.Count(x => x.Resultado == "Error" && x.Error == "QA_UPSTREAM_CONTRACT"));
        var recoveredAudit = Assert.Single(audits.Where(x => x.Resultado == "Exito" &&
            (x.ValoresNuevos ?? "").Contains("\"CI_MINIMA_ACREDITADA\"", StringComparison.Ordinal)));
        Assert.Null(recoveredAudit.EmpresaId);
        Assert.Equal(AmbitoAutorizacion.Plataforma, recoveredAudit.AmbitoAutoridad);
        using var recoveredMetadata = JsonDocument.Parse(recoveredAudit.ValoresNuevos!);
        Assert.Equal(4, recoveredMetadata.RootElement.GetProperty("ChecksIdentificados").GetArrayLength());
        Assert.All(audits, x => Assert.DoesNotContain("private-secret", x.ValoresNuevos ?? ""));
    }

    [Fact]
    public async Task GlobalDirectedBuildIdentityIsPersistedOnlyAfterProviderJoin()
    {
        const string root = "/repos/solqaryn/Solqaryn/";
        const string directedName = "C01-04.M03.06 - directed durable MySQL 8.4 QA proof";
        const string details = "https://github.com/solqaryn/Solqaryn/actions/runs/123/job/7899";
        const string checkLink = "https://api.github.com/repos/solqaryn/Solqaryn/check-runs/460";
        using var existing = JsonDocument.Parse(Payload());
        var directed = new
        {
            id = 460L, name = directedName, head_sha = Sha, status = "completed",
            conclusion = "success", details_url = details, check_suite = new { id = 793L },
            app = new { id = 15368L, slug = "github-actions" }
        };
        var combined = JsonSerializer.Serialize(new
        {
            total_count = 5,
            check_runs = existing.RootElement.GetProperty("check_runs").EnumerateArray()
                .Select(x => (object)x.Clone()).Append(directed).ToArray()
        });
        var job = new
        {
            id = 7899L, run_id = 123L, run_attempt = 2, head_sha = Sha, head_branch = "dev",
            name = directedName, status = "completed", conclusion = "success",
            html_url = details, check_run_url = checkLink
        };
        var jobs = JsonSerializer.Serialize(new { total_count = 1, jobs = new[] { job } });
        var attempt = JsonSerializer.Serialize(new
        {
            id = 123L, run_attempt = 2, head_sha = Sha, head_branch = "dev", @event = "push",
            path = ".github/workflows/v23-c01-04-m03-06-durable-qa-mysql84.yml",
            repository = new { full_name = "solqaryn/Solqaryn" },
            head_repository = new { full_name = "solqaryn/Solqaryn" },
            workflow_id = 426472921L, check_suite_id = 793L, status = "completed", conclusion = "success"
        });
        var discovery = root + "actions/runs/123/jobs?filter=all&per_page=100";
        var upstream = new GitHubFake(combined);
        upstream.AdditionalResponses[discovery] = jobs;
        upstream.AdditionalResponses[root + "actions/runs/123/attempts/2"] = attempt;
        upstream.AdditionalResponses[root + "actions/runs/123/attempts/2/jobs?per_page=100"] = jobs;
        upstream.AdditionalResponses[root + "commits/" + Sha + "/check-runs?per_page=100&filter=all"] = combined;
        await using var fixture = await CrearAsync(upstream, mysql: !string.IsNullOrWhiteSpace(
            Environment.GetEnvironmentVariable("SOLQARYN_C0204_MYSQL_TEST_CONNECTION")));
        using var client = fixture.Host.GetTestClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", Token(fixture.GlobalId, "operadorglobal"));
        using (var response = await client.GetAsync(Path))
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal("SIN_VERIFICAR", json.RootElement.GetProperty("data").GetProperty("estadoA5").GetString());
        }
        Assert.Equal(8, upstream.Calls);

        // A valid-looking check link to another check must not become a build receipt.
        upstream.AdditionalResponses[discovery] = jobs.Replace(checkLink,
            "https://api.github.com/repos/solqaryn/Solqaryn/check-runs/461", StringComparison.Ordinal);
        using (var denied = await client.GetAsync(Path))
            Assert.Equal(HttpStatusCode.ServiceUnavailable, denied.StatusCode);

        using var scope = fixture.Host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var audits = await db.Set<RegistroAuditoria>().ToListAsync();
        var success = Assert.Single(audits.Where(x => x.Resultado == "Exito"));
        Assert.Null(success.EmpresaId);
        Assert.Equal(AmbitoAutorizacion.Plataforma, success.AmbitoAutoridad);
        using var metadata = JsonDocument.Parse(success.ValoresNuevos!);
        Assert.Equal("/plataforma/gestion/qa/evidencias/{sha}",
            metadata.RootElement.GetProperty("RutaConsulta").GetString());
        var build = Assert.Single(metadata.RootElement.GetProperty("ChecksIdentificados").EnumerateArray()
            .Where(x => x.GetProperty("CheckId").GetInt64() == 460)).GetProperty("Build");
        Assert.Equal("C01-04.M03.06", build.GetProperty("MicrotaskId").GetString());
        Assert.Equal(Sha, build.GetProperty("SourceSha").GetString());
        Assert.Equal(123L, build.GetProperty("RunId").GetInt64());
        Assert.Equal(2, build.GetProperty("Attempt").GetInt32());
        Assert.Equal(7899L, build.GetProperty("JobId").GetInt64());
        Assert.Equal(460L, build.GetProperty("CheckRunId").GetInt64());
        Assert.Equal(793L, build.GetProperty("CheckSuiteId").GetInt64());
        Assert.Single(audits.Where(x => x.Resultado == "Error" && x.Error == "QA_UPSTREAM_CONTRACT"));
        Assert.All(audits, x => Assert.Null(x.EmpresaId));

        upstream.AdditionalResponses[discovery] = jobs;
        await AssertNoSecondaryCallsAfterRevocationAsync(upstream, fixture, client);
        await AssertRevocationDuringSecondaryReadAsync(upstream);
    }

    [Theory]
    [InlineData("valid", "CI_MINIMA_ACREDITADA", HttpStatusCode.OK)]
    [InlineData("reuse", "CI_MINIMA_ACREDITADA", HttpStatusCode.OK)]
    [InlineData("wrong-workflow", null, HttpStatusCode.ServiceUnavailable)]
    [InlineData("wrong-check", null, HttpStatusCode.ServiceUnavailable)]
    [InlineData("wrong-suite", null, HttpStatusCode.ServiceUnavailable)]
    [InlineData("wrong-sha", null, HttpStatusCode.ServiceUnavailable)]
    [InlineData("wrong-step", null, HttpStatusCode.ServiceUnavailable)]
    [InlineData("duplicate", "SIN_VERIFICAR", HttpStatusCode.OK)]
    [InlineData("wrong-name", "SIN_VERIFICAR", HttpStatusCode.OK)]
    [InlineData("skipped", "SIN_VERIFICAR", HttpStatusCode.OK)]
    [InlineData("failure", "CI_FALLIDA", HttpStatusCode.OK)]
    public async Task CoreBackendGateRequiresCanonicalWorkflowAndExactJob(
        string scenario, string? expected, HttpStatusCode httpStatus)
    {
        const string root = "/repos/solqaryn/Solqaryn/";
        const string core = "SOLQARYN - Build y pruebas backend";
        const string details = "https://github.com/solqaryn/Solqaryn/actions/runs/123/job/7899";
        const string checkLink = "https://api.github.com/repos/solqaryn/Solqaryn/check-runs/459";
        var conclusion = scenario is "failure" or "skipped" ? scenario : "success";
        var payload = Payload(conclusion, [details, details, details, details])
            .Replace("Backend Release y pruebas", scenario == "wrong-name" ? core + " copia" : core,
                StringComparison.Ordinal);
        if (scenario == "wrong-sha")
            payload = payload.Replace(Sha, new string('a', 40), StringComparison.Ordinal);
        if (scenario == "duplicate")
        {
            using var data = JsonDocument.Parse(payload);
            var checks = data.RootElement.GetProperty("check_runs").EnumerateArray().ToArray();
            using var duplicate = JsonDocument.Parse(checks[3].GetRawText()
                .Replace("\"id\":459", "\"id\":460", StringComparison.Ordinal));
            payload = JsonSerializer.Serialize(new { total_count = 5,
                check_runs = checks.Append(duplicate.RootElement).ToArray() });
        }
        var job = new
        {
            id = 7899L, run_id = 123L, run_attempt = 2, head_sha = Sha, head_branch = "dev",
            name = core, status = "completed", conclusion = "success", html_url = details,
            check_run_url = scenario == "wrong-check" ? checkLink.Replace("459", "460") : checkLink,
            steps = new[]
            {
                new { name = "Restaurar, compilar y probar sin imponer fases previas",
                    status = "completed", conclusion = scenario == "reuse" ? "skipped" :
                        scenario == "wrong-step" ? "failure" : "success" },
                new { name = "Acreditar resultado backend DEV exact-SHA (no repetir 6500+ pruebas)",
                    status = "completed", conclusion = scenario == "reuse" ? "success" : "skipped" },
                new { name = "No sustituir un backend DEV fallido por PASS",
                    status = "completed", conclusion = "skipped" }
            }
        };
        var jobs = JsonSerializer.Serialize(new { total_count = 1, jobs = new[] { job } });
        var attempt = JsonSerializer.Serialize(new
        {
            id = 123L, run_attempt = 2, head_sha = Sha, head_branch = "dev", @event = "push",
            path = scenario == "wrong-workflow" ? ".github/workflows/untrusted.yml" :
                ".github/workflows/solqaryn-core-ci.yml",
            workflow_id = 393193776L, check_suite_id = scenario == "wrong-suite" ? 793L : 792L,
            repository = new { full_name = "solqaryn/Solqaryn" },
            head_repository = new { full_name = "solqaryn/Solqaryn" }
        });
        var upstream = new GitHubFake(payload);
        upstream.AdditionalResponses[root + "actions/runs/123/jobs?filter=all&per_page=100"] = jobs;
        upstream.AdditionalResponses[root + "actions/runs/123/attempts/2"] = attempt;
        upstream.AdditionalResponses[root + "actions/runs/123/attempts/2/jobs?per_page=100"] = jobs;
        await using var fixture = await CrearAsync(upstream);
        using var client = fixture.Host.GetTestClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", Token(fixture.GlobalId, "operadorglobal"));
        using var response = await client.GetAsync(Path);
        Assert.Equal(httpStatus, response.StatusCode);
        if (expected is not null)
        {
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal(expected, json.RootElement.GetProperty("data").GetProperty("estadoCi").GetString());
            Assert.Equal("SIN_VERIFICAR", json.RootElement.GetProperty("data").GetProperty("estadoA5").GetString());
        }
        if (scenario is "valid" or "reuse")
        {
            Assert.Equal(4, upstream.Calls);
            using var scope = fixture.Host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var audit = Assert.Single(await db.Set<RegistroAuditoria>().ToListAsync());
            Assert.Null(audit.EmpresaId);
            using var metadata = JsonDocument.Parse(audit.ValoresNuevos!);
            var identity = Assert.Single(metadata.RootElement.GetProperty("ChecksIdentificados")
                .EnumerateArray().Where(x => x.GetProperty("CheckId").GetInt64() == 459))
                .GetProperty("CoreBackend");
            Assert.Equal(393193776L, identity.GetProperty("WorkflowId").GetInt64());
            Assert.Equal(2, identity.GetProperty("Attempt").GetInt32());
            Assert.Equal(459L, identity.GetProperty("CheckRunId").GetInt64());
            Assert.Equal(scenario == "reuse" ? "EXACT_SHA_REUSE" : "BUILD_TEST",
                identity.GetProperty("Mode").GetString());
            if (scenario == "valid")
            {
                await AssertNoSecondaryCallsAfterRevocationAsync(upstream, fixture, client);
                await AssertRevocationDuringSecondaryReadAsync(upstream);
            }
        }
    }

    [Theory]
    [InlineData("success", "success", false, "CI_MINIMA_ACREDITADA")]
    [InlineData("skipped", "success", false, "SIN_VERIFICAR")]
    [InlineData("failure", "success", false, "CI_FALLIDA")]
    [InlineData("success", "success", true, "SIN_VERIFICAR")]
    [InlineData("success", null, false, "CI_EN_PROGRESO")]
    [InlineData("success", "failure", false, "CI_FALLIDA")]
    [InlineData("success", "skipped", false, "CI_MINIMA_ACREDITADA")]
    public async Task CoexistingBackendGatesNeverFallbackFromHistoricalDev(
        string devConclusion, string? coreConclusion, bool duplicateCore, string expected)
    {
        const string core = "SOLQARYN - Build y pruebas backend";
        using var basePayload = JsonDocument.Parse(Payload(devConclusion));
        var checks = basePayload.RootElement.GetProperty("check_runs").EnumerateArray().ToList();
        JsonElement CoreCheck(long id) => JsonSerializer.SerializeToElement(new
        {
            id,
            check_suite = new { id = 793L },
            app = new { id = 15368L, slug = "github-actions" },
            head_sha = Sha,
            name = core,
            status = coreConclusion is null ? "in_progress" : "completed",
            conclusion = coreConclusion,
            details_url = "https://github.com/solqaryn/Solqaryn/actions/runs/123/job/7899"
        });
        checks.Add(CoreCheck(460L));
        if (duplicateCore) checks.Add(CoreCheck(461L));
        var upstream = new GitHubFake(JsonSerializer.Serialize(new
        {
            total_count = checks.Count,
            check_runs = checks
        }));
        await using var fixture = await CrearAsync(upstream);
        using var client = fixture.Host.GetTestClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", Token(fixture.GlobalId, "operadorglobal"));
        using var response = await client.GetAsync(Path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(expected, json.RootElement.GetProperty("data").GetProperty("estadoCi").GetString());
        Assert.Equal("SIN_VERIFICAR", json.RootElement.GetProperty("data").GetProperty("estadoA5").GetString());

        // Only the legacy job may be joined; never substitute the core job.
        Assert.Equal(expected == "CI_MINIMA_ACREDITADA" ? 4 : 1, upstream.Calls);
        using var scope = fixture.Host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var audit = Assert.Single(await db.Set<RegistroAuditoria>().ToListAsync());
        Assert.Null(audit.EmpresaId);
        Assert.Equal(AmbitoAutorizacion.Plataforma, audit.AmbitoAutoridad);
        using var metadata = JsonDocument.Parse(audit.ValoresNuevos!);
        var coreIdentities = metadata.RootElement.GetProperty("ChecksIdentificados").EnumerateArray()
            .Where(x => x.GetProperty("CheckId").GetInt64() is 460L or 461L).ToArray();
        Assert.Equal(duplicateCore ? 2 : 1, coreIdentities.Length);
        Assert.All(coreIdentities, x => Assert.True(
            !x.TryGetProperty("CoreBackend", out var receipt) || receipt.ValueKind == JsonValueKind.Null));
    }

    [Theory]
    [InlineData("wrong-workflow")]
    [InlineData("wrong-check")]
    [InlineData("wrong-step")]
    [InlineData("invalid-link")]
    public async Task LegacyBackendRequiresExactBuildProvenance(string scenario)
    {
        var payload = Payload();
        if (scenario == "invalid-link")
            payload = payload.Replace("https://github.com/solqaryn/Solqaryn/actions/runs/124/job/7900",
                "https://github.com:8443/solqaryn/Solqaryn/actions/runs/124/job/7900", StringComparison.Ordinal);
        var upstream = new GitHubFake(payload) { LegacyScenario = scenario };
        await using var fixture = await CrearAsync(upstream);
        using var client = fixture.Host.GetTestClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", Token(fixture.GlobalId, "operadorglobal"));
        using var response = await client.GetAsync(Path);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.DoesNotContain("CI_MINIMA_ACREDITADA", await response.Content.ReadAsStringAsync());
        using var scope = fixture.Host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var audit = Assert.Single(await db.Set<RegistroAuditoria>().ToListAsync());
        Assert.Equal("Error", audit.Resultado);
        Assert.Equal("QA_UPSTREAM_CONTRACT", audit.Error);
        Assert.Null(audit.EmpresaId);
    }

    private static string Payload(string conclusion = "success", string[]? detailsUrls = null)
    {
        var checkIndex = 0;
        object Check(string name, string result)
        {
            var index = checkIndex++;
            return new
            {
                id = 456L + index,
                check_suite = new { id = 789L + index },
                app = new { id = 15368L, slug = "github-actions" },
                head_sha = Sha,
                name,
                status = "completed",
                conclusion = result,
                details_url = detailsUrls is null ? (index == 3 ? "https://github.com/solqaryn/Solqaryn/actions/runs/124/job/7900" : "https://github.com/solqaryn/Solqaryn/actions/runs/123/job/456") : detailsUrls[index],
                output = new { summary = "private-secret-not-for-the-response" }
            };
        }
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

    private static async Task<Fixture> CrearAsync(GitHubFake upstream, bool mysql = false,
        DenialReadbackFault? denialReadbackFault = null)
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
                        if (denialReadbackFault is not null)
                            opts.AddInterceptors(denialReadbackFault);
                    });
                    services.AddScoped<ICurrentUserService, CurrentUserService>();
                    services.AddScoped<IPlataformaScopeService, PlataformaScopeService>();
                    services.AddHttpClient("SolqarynPublicGitHubQa",
                            c =>
                            {
                                c.BaseAddress = new Uri("https://api.github.com/");
                                c.Timeout = upstream.ProviderTimeout;
                            })
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

    private sealed class DenialReadbackFault : DbCommandInterceptor
    {
        public bool Enabled { get; set; }
        public int Failures { get; private set; }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            if (Enabled &&
                command.CommandText.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase) &&
                command.CommandText.Contains("`RegistrosAuditoria`", StringComparison.Ordinal))
            {
                Failures++;
                command.CommandText = command.CommandText.Replace(
                    "`RegistrosAuditoria`", "`qa_ci_missing_denial_receipt_table`",
                    StringComparison.Ordinal);
            }
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
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

    private static async Task AssertRevocationDuringSecondaryReadAsync(GitHubFake template)
    {
        // A new host, database and persisted grant: never reuse the revoked grant.
        var upstream = new GitHubFake(template.Payload) { LegacyScenario = template.LegacyScenario };
        foreach (var entry in template.AdditionalResponses)
            upstream.AdditionalResponses.Add(entry.Key, entry.Value);
        await using var fixture = await CrearAsync(upstream, mysql: !string.IsNullOrWhiteSpace(
            Environment.GetEnvironmentVariable("SOLQARYN_C0204_MYSQL_TEST_CONNECTION")));
        using var client = fixture.Host.GetTestClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", Token(fixture.GlobalId, "operadorglobal"));
        await AssertNoSecondaryCallsAfterRevocationAsync(upstream, fixture, client, revokeOnCall: 2);
    }

    private static async Task AssertNoSecondaryCallsAfterRevocationAsync(
        GitHubFake upstream, Fixture fixture, HttpClient client, int revokeOnCall = 1)
    {
        var callsBefore = upstream.Calls;
        int auditsBefore;
        using (var beforeScope = fixture.Host.Services.CreateScope())
        {
            var beforeDb = beforeScope.ServiceProvider.GetRequiredService<AppDbContext>();
            auditsBefore = await beforeDb.Set<RegistroAuditoria>().CountAsync();
        }
        upstream.BeforeResponse = async () =>
        {
            if (upstream.Calls != callsBefore + revokeOnCall) return;
            upstream.BeforeResponse = null;
            using var revokedScope = fixture.Host.Services.CreateScope();
            var revokedDb = revokedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var grant = await revokedDb.Set<UsuarioRolPlataforma>().SingleAsync(
                x => x.UsuarioId == fixture.GlobalId);
            grant.Desactivar();
            await revokedDb.SaveChangesAsync();
        };
        using (var revoked = await client.GetAsync(Path))
        {
            Assert.Equal(HttpStatusCode.Forbidden, revoked.StatusCode);
            Assert.DoesNotContain("CI_MINIMA_ACREDITADA", await revoked.Content.ReadAsStringAsync());
        }
        Assert.Equal(callsBefore + revokeOnCall, upstream.Calls);
        using var scope = fixture.Host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(auditsBefore + 1, await db.Set<RegistroAuditoria>().CountAsync());
        var denial = Assert.Single(await db.Set<RegistroAuditoria>().AsNoTracking()
            .Where(x => x.Error == "QA_ACCESS_DENIED").ToListAsync());
        AssertDenialAudit(denial, fixture.GlobalId);
    }

    private sealed class GitHubFake(string payload) : HttpMessageHandler
    {
        public int Calls { get; private set; }
        public string? LastHost { get; private set; }
        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;
        public string Payload { get; set; } = payload;
        public bool OmitContentLength { get; set; }
        public TimeSpan ProviderTimeout { get; set; } = TimeSpan.FromSeconds(100);
        public StalledQaBody? StalledBody { get; set; }
        public Func<Task>? BeforeResponse { get; set; }
        public Dictionary<string, string> AdditionalResponses { get; } = new(StringComparer.Ordinal);
        public string? LegacyScenario { get; set; }

        private string? LegacyResponse(string path)
        {
            const string run = "/repos/solqaryn/Solqaryn/actions/runs/124";
            const string details = "https://github.com/solqaryn/Solqaryn/actions/runs/124/job/7900";
            if (path == run + "/attempts/1")
                return JsonSerializer.Serialize(new
                {
                    id = 124L, run_attempt = 1, workflow_id = LegacyScenario == "wrong-workflow" ? 1L : 366460517L,
                    check_suite_id = 792L, path = ".github/workflows/dev-ci.yml",
                    head_sha = Sha, head_branch = "dev", @event = "push",
                    repository = new { full_name = "solqaryn/Solqaryn" },
                    head_repository = new { full_name = "solqaryn/Solqaryn" }
                });
            if (path != run + "/jobs?filter=all&per_page=100" &&
                path != run + "/attempts/1/jobs?per_page=100") return null;
            var steps = new[]
            {
                new { name = "Run dotnet restore Solqaryn.sln", status = "completed", conclusion = "success" },
                new { name = "Run dotnet build Solqaryn.sln --configuration Release --no-restore", status = "completed", conclusion = "success" },
                new { name = "Run dotnet test Solqaryn.sln --configuration Release --no-build --filter \"Category!=Integration\" --logger \"trx;LogFileName=tests.trx\"", status = "completed", conclusion = LegacyScenario == "wrong-step" ? "skipped" : "success" }
            };
            return JsonSerializer.Serialize(new { total_count = 1, jobs = new[] { new
            {
                id = 7900L, run_id = 124L, run_attempt = 1,
                head_sha = Sha, head_branch = "dev", name = "Backend Release y pruebas",
                status = "completed", conclusion = "success", html_url = details,
                check_run_url = "https://api.github.com/repos/solqaryn/Solqaryn/check-runs/" +
                    (LegacyScenario == "wrong-check" ? "460" : "459"),
                steps
            } } });
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            LastHost = request.RequestUri?.Host;
            if (LastHost != "api.github.com")
                throw new InvalidOperationException("Egreso fuera del repositorio canónico.");
            if (BeforeResponse is not null)
                await BeforeResponse();
            if (AdditionalResponses.TryGetValue(request.RequestUri!.PathAndQuery, out var additional))
                return new HttpResponseMessage(StatusCode)
                {
                    Content = new StringContent(additional, Encoding.UTF8, "application/json")
                };
            var legacy = LegacyResponse(request.RequestUri!.PathAndQuery);
            if (legacy is not null)
                return new HttpResponseMessage(StatusCode)
                {
                    Content = new StringContent(legacy, Encoding.UTF8, "application/json")
                };
            if (request.RequestUri.AbsolutePath !=
                "/repos/solqaryn/Solqaryn/commits/" + Sha + "/check-runs")
                throw new InvalidOperationException("Egreso fuera del repositorio canónico.");
            return new HttpResponseMessage(StatusCode)
            {
                Content = StalledBody is not null
                    ? new StreamContent(StalledBody)
                    : OmitContentLength
                    ? new ChunkedQaContent(Payload)
                    : new StringContent(Payload, Encoding.UTF8, "application/json")
            };
        }
    }

    private sealed class StalledQaBody : Stream
    {
        public bool ReadStarted { get; private set; }
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }
        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override async ValueTask<int> ReadAsync(
            Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            ReadStarted = true;
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return 0;
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count,
            CancellationToken cancellationToken) =>
            ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
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

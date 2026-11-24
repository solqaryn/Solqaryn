using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Solqaryn.API.Middleware;
using Solqaryn.Application.Common;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;

namespace Solqaryn.API.Controllers;

/// <summary>
/// Solo metadatos de checks reales de GitHub Actions sobre un SHA inmutable.
/// No interpreta estos checks como A5, permisos de tenant ni pruebas de producción.
/// </summary>
[ApiController]
[Authorize]
[Route("plataforma/gestion/qa/evidencias")]
public sealed class PlataformaQaEvidenciasController : ControllerBase
{
    private static readonly Regex ShaValido = new(
        @"\A[0-9a-fA-F]{40}\z", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    // Gates mínimos para un cambio backend; los checks adicionales se muestran
    // con su conclusión REAL y nunca se convierten de SKIPPED a PASS.
    private static readonly string[] ChecksMinimos =
    [
        "verify-solqaryn-project-scope",
        "V5 - Sin encabezados vacios ni atribucion cruzada",
        "SOLQARYN - Seguridad multiempresa",
        "Backend Release y pruebas"
    ];

    private readonly IPlataformaScopeService _scope;
    private readonly ICurrentUserService _current;
    private readonly IHttpClientFactory _clients;
    private readonly AppDbContext _db;

    public PlataformaQaEvidenciasController(
        IPlataformaScopeService scope,
        ICurrentUserService current,
        IHttpClientFactory clients,
        AppDbContext db)
    {
        _scope = scope;
        _current = current;
        _clients = clients;
        _db = db;
    }

    [HttpGet("{sha}")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Consultar(string sha, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "private, no-store, max-age=0";
        if (User.Identity?.IsAuthenticated != true ||
            !_current.EstaAutenticado || _current.UsuarioId is not > 0)
            return Forbid();

        // JWT identifica; la autorización efectiva se reconsulta en la DB en
        // CADA petición. Admin empresarial, X-Empresa-Id y claims no elevan rol.
        var actor = await _scope.ObtenerActualAsync(cancellationToken);
        if (actor is null || actor.UsuarioId != _current.UsuarioId) return Forbid();

        // The provider can take time. A revoked or changed global grant must
        // not authorize disclosure or a successful audit after that await.
        async Task<bool> VigenteAsync()
        {
            var actual = await _scope.ObtenerActualAsync(cancellationToken);
            return actual is not null &&
                actual.UsuarioId == actor.UsuarioId &&
                actual.RolId == actor.RolId &&
                string.Equals(actual.NombreUsuario, actor.NombreUsuario, StringComparison.Ordinal);
        }

        if (string.IsNullOrEmpty(sha) || !ShaValido.IsMatch(sha))
            return BadRequest(ApiResponse<object>.Fail("SHA de evidencia inválido."));

        sha = sha.ToLowerInvariant();

        // Only an actor whose platform grant was revalidated may create this audit.
        // Persist a bounded error code, never the upstream body, URL, exception or JWT.
        async Task<IActionResult> FalloProveedorAsync(string codigo, bool notFound = false)
        {
            if (!await VigenteAsync()) return Forbid();
            try
            {
                _db.Set<RegistroAuditoria>().Add(new RegistroAuditoria
                {
                    UsuarioId = actor.UsuarioId,
                    NombreUsuario = actor.NombreUsuario,
                    EmpresaId = null,
                    AmbitoAutoridad = AmbitoAutorizacion.Plataforma,
                    OrigenAutorizacion = OrigenAutorizacion.RolPlataforma,
                    Modulo = ModuloSistema.Configuracion,
                    Accion = AccionPermiso.ConsultarHistorial,
                    Entidad = "ConsultaEvidenciasQaGlobal",
                    Descripcion = "Consulta de evidencia QA no verificable.",
                    CorrelationId = CorrelacionAuditoria(),
                    ValoresNuevos = JsonSerializer.Serialize(new
                    {
                        Schema = "qa-check-evidence/v1",
                        Sha = sha,
                        EstadoCi = "SIN_VERIFICAR",
                        EstadoA5 = "SIN_VERIFICAR",
                        TotalChecks = 0,
                        ChecksExitosos = 0
                    }),
                    Resultado = "Error",
                    Error = codigo
                });
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                return NoVerificable();
            }
            return notFound
                ? NotFound(ApiResponse<object>.Fail("Revisión no encontrada."))
                : NoVerificable();
        }

        try
        {
            // Destino fijo: ni el cliente ni el SHA pueden introducir hosts o URLs.
            using var response = await _clients.CreateClient("SolqarynPublicGitHubQa")
                .GetAsync("repos/solqaryn/Solqaryn/commits/" + sha + "/check-runs?per_page=100",
                    HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return await FalloProveedorAsync("QA_UPSTREAM_NOT_FOUND", notFound: true);
            const int maxGithubResponseBytes = 512_000;
            if (!response.IsSuccessStatusCode ||
                response.Content.Headers.ContentLength is > maxGithubResponseBytes)
                return await FalloProveedorAsync("QA_UPSTREAM_UNAVAILABLE");

            // Content-Length may be absent for Transfer-Encoding: chunked.
            // Bound the actual bytes consumed before JSON parsing, including the
            // one-byte overflow probe. Never buffer an unbounded upstream body.
            await using var content = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var bounded = new MemoryStream();
            var chunk = new byte[16_384];
            while (true)
            {
                var maxRead = Math.Min(chunk.Length, maxGithubResponseBytes + 1 - (int)bounded.Length);
                var read = await content.ReadAsync(chunk.AsMemory(0, maxRead), cancellationToken);
                if (read == 0) break;
                if (bounded.Length + read > maxGithubResponseBytes)
                    return await FalloProveedorAsync("QA_UPSTREAM_SIZE");
                await bounded.WriteAsync(chunk.AsMemory(0, read), cancellationToken);
            }
            bounded.Position = 0;
            using var document = await JsonDocument.ParseAsync(
                bounded, new JsonDocumentOptions { MaxDepth = 20 }, cancellationToken);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object ||
                !root.TryGetProperty("total_count", out var countElement) ||
                countElement.ValueKind != JsonValueKind.Number ||
                !countElement.TryGetInt32(out var count) || count < 1 || count > 100 ||
                !root.TryGetProperty("check_runs", out var checkRuns) ||
                checkRuns.ValueKind != JsonValueKind.Array ||
                checkRuns.GetArrayLength() != count)
                return await FalloProveedorAsync("QA_UPSTREAM_CONTRACT");

            var checks = new List<PlataformaQaCheckDto>(count);
            foreach (var item in checkRuns.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object ||
                    !string.Equals(Valor(item, "head_sha"), sha, StringComparison.OrdinalIgnoreCase))
                    return await FalloProveedorAsync("QA_UPSTREAM_CONTRACT");

                var name = Valor(item, "name");
                var status = Valor(item, "status");
                if (string.IsNullOrWhiteSpace(name) || name.Length > 180 ||
                    status is not ("queued" or "in_progress" or "completed" or "waiting" or "requested" or "pending"))
                    return await FalloProveedorAsync("QA_UPSTREAM_CONTRACT");

                var conclusion = Valor(item, "conclusion");
                var details = Valor(item, "details_url");
                string? safeUrl = null;
                if (Uri.TryCreate(details, UriKind.Absolute, out var parsed) &&
                    parsed.Scheme == Uri.UriSchemeHttps &&
                    string.Equals(parsed.Host, "github.com", StringComparison.OrdinalIgnoreCase) &&
                    parsed.AbsolutePath.StartsWith("/solqaryn/Solqaryn/actions/", StringComparison.Ordinal))
                    safeUrl = parsed.ToString();

                checks.Add(new PlataformaQaCheckDto(name, status, conclusion, safeUrl));
            }

            var failed = checks.Any(c => c.Conclusion is
                ("failure" or "cancelled" or "timed_out" or "action_required"));
            var pending = checks.Any(c => c.Estado != "completed");
            var allRequired = ChecksMinimos.All(name =>
                checks.Count(c => string.Equals(c.Nombre, name, StringComparison.Ordinal)) == 1 &&
                checks.Any(c => c.Nombre == name && c.Estado == "completed" &&
                                c.Conclusion == "success"));

            var estado = failed ? "CI_FALLIDA" :
                pending ? "CI_EN_PROGRESO" :
                allRequired ? "CI_MINIMA_ACREDITADA" : "SIN_VERIFICAR";

            var successfulChecks = checks.Count(c => c.Estado == "completed" && c.Conclusion == "success");
            if (!await VigenteAsync()) return Forbid();

            // Auditoría estructurada: únicamente metadatos acotados, sin cuerpo externo ni credenciales.
            _db.Set<RegistroAuditoria>().Add(new RegistroAuditoria
            {
                UsuarioId = actor.UsuarioId,
                NombreUsuario = actor.NombreUsuario,
                EmpresaId = null,
                AmbitoAutoridad = AmbitoAutorizacion.Plataforma,
                OrigenAutorizacion = OrigenAutorizacion.RolPlataforma,
                Modulo = ModuloSistema.Configuracion,
                Accion = AccionPermiso.ConsultarHistorial,
                Entidad = "ConsultaEvidenciasQaGlobal",
                Descripcion = "Consulta privada de checks del commit " + sha[..12] + ".",
                CorrelationId = CorrelacionAuditoria(),
                ValoresNuevos = JsonSerializer.Serialize(new
                {
                    Schema = "qa-check-evidence/v1",
                    Sha = sha,
                    EstadoCi = estado,
                    EstadoA5 = "SIN_VERIFICAR",
                    TotalChecks = checks.Count,
                    ChecksExitosos = successfulChecks
                }),
                Resultado = "Exito"
            });
            await _db.SaveChangesAsync(cancellationToken);

            return Ok(ApiResponse<PlataformaQaEvidenciasDto>.Ok(
                new PlataformaQaEvidenciasDto(
                    "solqaryn/Solqaryn", sha, estado,
                    "SIN_VERIFICAR", checks.Count,
                    successfulChecks,
                    checks)));
        }
        catch (DbUpdateException)
        {
            // No devolver evidencia si no se pudo persistir su auditoría.
            return NoVerificable();
        }
        catch (HttpRequestException)
        {
            return await FalloProveedorAsync("QA_UPSTREAM_TRANSPORT");
        }
        catch (IOException)
        {
            return await FalloProveedorAsync("QA_UPSTREAM_TRANSPORT");
        }
        catch (JsonException)
        {
            return await FalloProveedorAsync("QA_UPSTREAM_CONTRACT");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return await FalloProveedorAsync("QA_UPSTREAM_TIMEOUT");
        }
    }

    // Reuse only the correlation value normalized by the HTTP middleware.
    // It is diagnostic metadata, never an authority or an idempotency key.
    private string CorrelacionAuditoria()
    {
        if (HttpContext.Items[CorrelationIdMiddleware.ItemKey] is string value &&
            value.Length is >= 1 and <= 64 &&
            value.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.' or ':'))
            return value;

        return Guid.NewGuid().ToString("N");
    }

    private ObjectResult NoVerificable() =>
        StatusCode(StatusCodes.Status503ServiceUnavailable,
            ApiResponse<object>.Fail("Evidencia externa temporalmente no verificable."));

    private static string? Valor(JsonElement item, string key) =>
        item.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() : null;
}

public sealed record PlataformaQaCheckDto(
    string Nombre, string Estado, string? Conclusion, string? UrlEvidencia);

public sealed record PlataformaQaEvidenciasDto(
    string Repositorio,
    string Sha,
    string EstadoCi,
    string EstadoA5,
    int TotalChecks,
    int ChecksExitosos,
    IReadOnlyList<PlataformaQaCheckDto> Checks);

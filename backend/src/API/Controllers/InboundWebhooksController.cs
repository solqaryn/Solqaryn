using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Solqaryn.Api.Controllers;

[ApiController]
[Route("api/webhooks/{empresaId:int}/{proveedor}")]
public sealed class InboundWebhooksController : ControllerBase
{
    private const string SignatureHeader = "X-Webhook-Signature";
    private const int MaxPayloadBytes = 256 * 1024;

    private readonly AppDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly ILogger<InboundWebhooksController> _logger;

    public InboundWebhooksController(
        AppDbContext db,
        IConfiguration configuration,
        ILogger<InboundWebhooksController> logger)
    {
        _db = db;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> ReceiveAsync(
        int empresaId,
        string proveedor,
        CancellationToken cancellationToken)
    {
        if (empresaId <= 0 || !InboundWebhookIngressService.TryNormalizeProvider(proveedor, out var normalizedProvider))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Solicitud de webhook inválida.",
                detail: "La identidad del tenant o proveedor no es válida.");
        }

        var secret = _configuration[$"Webhooks:{empresaId}:{normalizedProvider}:Secret"];
        if (string.IsNullOrWhiteSpace(secret))
        {
            _logger.LogWarning(
                "Webhook rechazado por configuración ausente para tenant {EmpresaId}, proveedor {Proveedor}. Correlación {CorrelationId}",
                empresaId,
                normalizedProvider,
                HttpContext.TraceIdentifier);

            return Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Webhook no configurado.",
                detail: "No existe una configuración activa para este origen.");
        }

        if (Request.ContentLength is > MaxPayloadBytes)
        {
            return PayloadTooLarge();
        }

        Request.EnableBuffering();
        var initialCapacity = Request.ContentLength is > 0
            ? (int)Request.ContentLength.Value
            : 0;
        using var bodyBuffer = new MemoryStream(initialCapacity);
        var readBuffer = new byte[8192];
        var totalBytes = 0;

        while (true)
        {
            var remainingProbeBytes = MaxPayloadBytes + 1 - totalBytes;
            var requestedBytes = Math.Min(readBuffer.Length, remainingProbeBytes);
            var bytesRead = await Request.Body.ReadAsync(
                readBuffer.AsMemory(0, requestedBytes),
                cancellationToken);

            if (bytesRead == 0)
                break;

            totalBytes += bytesRead;
            if (totalBytes > MaxPayloadBytes)
            {
                Request.Body.Position = 0;
                return PayloadTooLarge();
            }

            await bodyBuffer.WriteAsync(
                readBuffer.AsMemory(0, bytesRead),
                cancellationToken);
        }

        Request.Body.Position = 0;
        var rawBody = Encoding.UTF8.GetString(
            bodyBuffer.GetBuffer(),
            0,
            checked((int)bodyBuffer.Length));

        var signature = Request.Headers[SignatureHeader].ToString();
        var correlationId = HttpContext.TraceIdentifier;

        var service = new InboundWebhookIngressService(_db, secret);
        var result = await service.ReceiveAsync(
            empresaId,
            normalizedProvider,
            rawBody,
            signature,
            correlationId,
            cancellationToken);

        LogOutcome(result, empresaId, normalizedProvider, correlationId);

        return result.Kind switch
        {
            InboundWebhookIngressKind.Accepted => Accepted(new
            {
                result.WebhookId,
                Estado = "Recibido",
                Idempotente = false
            }),
            InboundWebhookIngressKind.Duplicate => Ok(new
            {
                result.WebhookId,
                Estado = "Recibido",
                Idempotente = true
            }),
            InboundWebhookIngressKind.InvalidSignature => Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Firma de webhook inválida.",
                detail: "La solicitud no superó la validación criptográfica."),
            InboundWebhookIngressKind.Conflict => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Webhook duplicado en conflicto.",
                detail: "El identificador externo ya existe con una huella de payload distinta."),
            _ => Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Solicitud de webhook inválida.",
                detail: "El cuerpo del webhook no cumple el contrato de entrada.")
        };
    }

    private ObjectResult PayloadTooLarge() => Problem(
        statusCode: StatusCodes.Status413PayloadTooLarge,
        title: "Payload de webhook demasiado grande.",
        detail: "El cuerpo excede el límite permitido para este endpoint.");

    private void LogOutcome(
        InboundWebhookIngressResult result,
        int empresaId,
        string normalizedProvider,
        string correlationId)
    {
        var level = result.Kind is InboundWebhookIngressKind.Accepted or InboundWebhookIngressKind.Duplicate
            ? LogLevel.Information
            : LogLevel.Warning;

        _logger.Log(
            level,
            "Webhook entrante procesado con resultado {Resultado} para tenant {EmpresaId}, proveedor {Proveedor}, webhook {WebhookId}. Correlación {CorrelationId}",
            result.Kind,
            empresaId,
            normalizedProvider,
            result.WebhookId,
            correlationId);
    }
}

public sealed class InboundWebhookIngressService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan MaxEventAge = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan MaxFutureSkew = TimeSpan.FromMinutes(1);

    private readonly AppDbContext _db;
    private readonly byte[] _secret;
    private readonly Func<DateTime> _utcNow;

    public InboundWebhookIngressService(AppDbContext db, string secret, Func<DateTime>? utcNow = null)
    {
        _db = db;
        _secret = Encoding.UTF8.GetBytes(secret ?? throw new ArgumentNullException(nameof(secret)));
        _utcNow = utcNow ?? (() => DateTime.UtcNow);
    }

    public async Task<InboundWebhookIngressResult> ReceiveAsync(
        int empresaId,
        string normalizedProvider,
        string rawBody,
        string? suppliedSignature,
        string? correlationId,
        CancellationToken cancellationToken)
    {
        if (!IsValidSignature(rawBody, suppliedSignature))
            return InboundWebhookIngressResult.InvalidSignature();

        InboundWebhookEnvelope? envelope;
        try
        {
            envelope = JsonSerializer.Deserialize<InboundWebhookEnvelope>(rawBody, JsonOptions);
        }
        catch (JsonException)
        {
            return InboundWebhookIngressResult.InvalidRequest();
        }

        if (envelope is null ||
            string.IsNullOrWhiteSpace(envelope.EventoExternoId) ||
            string.IsNullOrWhiteSpace(envelope.TipoEvento) ||
            envelope.EmitidoEnUtc.Kind != DateTimeKind.Utc)
        {
            return InboundWebhookIngressResult.InvalidRequest();
        }

        var receivedAtUtc = _utcNow();
        if (receivedAtUtc.Kind != DateTimeKind.Utc)
            receivedAtUtc = receivedAtUtc.ToUniversalTime();

        if (envelope.EmitidoEnUtc < receivedAtUtc - MaxEventAge ||
            envelope.EmitidoEnUtc > receivedAtUtc + MaxFutureSkew)
        {
            return InboundWebhookIngressResult.InvalidRequest();
        }

        var empresaExiste = await _db.Set<Empresa>()
            .AsNoTracking()
            .AnyAsync(x => x.Id == empresaId, cancellationToken);
        if (!empresaExiste)
            return InboundWebhookIngressResult.InvalidRequest();

        var payloadHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawBody)));

        var existente = await _db.Set<WebhookEntrante>()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.EmpresaId == empresaId &&
                     x.Proveedor == normalizedProvider &&
                     x.EventoExternoId == envelope.EventoExternoId.Trim(),
                cancellationToken);

        if (existente is not null)
        {
            return existente.CoincidePayload(payloadHash)
                ? InboundWebhookIngressResult.Duplicate(existente.Id)
                : InboundWebhookIngressResult.Conflict(existente.Id);
        }

        WebhookEntrante entity;
        try
        {
            entity = WebhookEntrante.CrearVerificado(
                empresaId,
                normalizedProvider,
                envelope.EventoExternoId,
                envelope.TipoEvento,
                payloadHash,
                correlationId,
                envelope.EmitidoEnUtc,
                receivedAtUtc);
        }
        catch (ArgumentException)
        {
            return InboundWebhookIngressResult.InvalidRequest();
        }

        _db.Set<WebhookEntrante>().Add(entity);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            return InboundWebhookIngressResult.Accepted(entity.Id);
        }
        catch (DbUpdateException)
        {
            _db.Entry(entity).State = EntityState.Detached;
            var concurrente = await _db.Set<WebhookEntrante>()
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x => x.EmpresaId == empresaId &&
                         x.Proveedor == normalizedProvider &&
                         x.EventoExternoId == envelope.EventoExternoId.Trim(),
                    cancellationToken);

            if (concurrente is null)
                throw;

            return concurrente.CoincidePayload(payloadHash)
                ? InboundWebhookIngressResult.Duplicate(concurrente.Id)
                : InboundWebhookIngressResult.Conflict(concurrente.Id);
        }
    }

    internal static bool TryNormalizeProvider(string? provider, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(provider))
            return false;

        var candidate = provider.Trim().ToLowerInvariant();
        if (candidate.Length > WebhookEntrante.LongitudMaximaProveedor ||
            candidate.Any(ch => !(char.IsLetterOrDigit(ch) || ch is '-' or '_' or '.')))
        {
            return false;
        }

        normalized = candidate;
        return true;
    }

    private bool IsValidSignature(string rawBody, string? suppliedSignature)
    {
        if (string.IsNullOrWhiteSpace(rawBody) || string.IsNullOrWhiteSpace(suppliedSignature))
            return false;

        var signature = suppliedSignature.Trim();
        if (signature.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase))
            signature = signature[7..];

        if (signature.Length != 64)
            return false;

        byte[] suppliedBytes;
        try
        {
            suppliedBytes = Convert.FromHexString(signature);
        }
        catch (FormatException)
        {
            return false;
        }

        if (suppliedBytes.Length != 32)
            return false;

        using var hmac = new HMACSHA256(_secret);
        var expected = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawBody));
        return CryptographicOperations.FixedTimeEquals(expected, suppliedBytes);
    }
}

public sealed class InboundWebhookEnvelope
{
    public string EventoExternoId { get; init; } = string.Empty;
    public string TipoEvento { get; init; } = string.Empty;
    public DateTime EmitidoEnUtc { get; init; }
}

public enum InboundWebhookIngressKind
{
    Accepted,
    Duplicate,
    InvalidSignature,
    Conflict,
    InvalidRequest
}

public sealed record InboundWebhookIngressResult(
    InboundWebhookIngressKind Kind,
    int? WebhookId)
{
    public static InboundWebhookIngressResult Accepted(int id) => new(InboundWebhookIngressKind.Accepted, id);
    public static InboundWebhookIngressResult Duplicate(int id) => new(InboundWebhookIngressKind.Duplicate, id);
    public static InboundWebhookIngressResult InvalidSignature() => new(InboundWebhookIngressKind.InvalidSignature, null);
    public static InboundWebhookIngressResult Conflict(int id) => new(InboundWebhookIngressKind.Conflict, id);
    public static InboundWebhookIngressResult InvalidRequest() => new(InboundWebhookIngressKind.InvalidRequest, null);
}

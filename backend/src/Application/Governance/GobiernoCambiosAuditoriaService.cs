using Microsoft.Extensions.Logging;
using Solqaryn.Domain.Security;

namespace Solqaryn.Application.Governance;

/// <summary>
/// C00-04.M03.06. Structured, bounded, non-authoritative audit of a governance
/// preparation query. It never approves, persists, executes, or changes rights.
/// There is no API, DI registration, worker, or external audit provider.
/// </summary>
public sealed class GobiernoCambiosAuditoriaService
{
    private static readonly EventId Accepted = new(4606, "GovernancePreparation");
    private static readonly EventId Rejected = new(4607, "GovernancePreparationDenied");

    private readonly GobiernoCambiosAuthorizationService _autorizacion;
    private readonly ILogger<GobiernoCambiosAuditoriaService> _logger;

    public GobiernoCambiosAuditoriaService(
        GobiernoCambiosAuthorizationService autorizacion,
        ILogger<GobiernoCambiosAuditoriaService> logger)
    {
        _autorizacion = autorizacion ?? throw new ArgumentNullException(nameof(autorizacion));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ResultadoPreparacionCambio> PrepararConsultaAuditadaAsync(
        ContextoTenantActual autenticado,
        int empresaId,
        SolicitudPreparacionCambio solicitud,
        Guid correlationId,
        int? sucursalId = null,
        CancellationToken cancellationToken = default)
    {
        if (correlationId == Guid.Empty)
            throw new ArgumentException("CorrelationId es obligatorio.", nameof(correlationId));

        // Do not grant even a preparatory result when the audit channel is off.
        // ILogger does not guarantee durable delivery; that requires a separate
        // approved implementation and is not certified by this microtask.
        if (!_logger.IsEnabled(LogLevel.Information) ||
            !_logger.IsEnabled(LogLevel.Warning))
            throw new InvalidOperationException("Canal de auditoria no disponible.");

        try
        {
            // Tenant and relational RBAC are checked BEFORE reading the local
            // projection. A result is always inert: no approval or execution.
            var resultado = await _autorizacion.PrepararConsultaAutorizadaAsync(
                autenticado, empresaId, solicitud, sucursalId, cancellationToken);

            // Fixed event type, correlation and allowlisted outcome only.
            // Never log actor, EmpresaId, reason, key, JWT, exception or payload.
            _logger.LogInformation(Accepted,
                "GobiernoCambios {Evento} {Correlacion} {Resultado}",
                "CONSULTA_PREPARACION", correlationId, resultado.Estado);
            return resultado;
        }
        catch (ArgumentException)
        {
            _logger.LogWarning(Rejected,
                "GobiernoCambios {Evento} {Correlacion} {Resultado}",
                "CONSULTA_RECHAZADA", correlationId, "ENTRADA_INVALIDA");
            throw;
        }
        catch (InvalidOperationException)
        {
            _logger.LogWarning(Rejected,
                "GobiernoCambios {Evento} {Correlacion} {Resultado}",
                "CONSULTA_RECHAZADA", correlationId, "ACCESO_DENEGADO_O_AUDITORIA");
            throw;
        }
    }
}

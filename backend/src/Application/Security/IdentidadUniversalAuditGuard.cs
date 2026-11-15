using Microsoft.Extensions.Logging;
using Solqaryn.Domain.Enums;
using Solqaryn.Domain.Security;

namespace Solqaryn.Application.Security;

/// <summary>
/// C01-01.M03.06: envoltura Application de la guardia de autorizacion actual.
/// No concede permisos, no expone endpoints ni escribe auditoria en persistencia.
/// </summary>
public sealed class IdentidadUniversalAuditGuard
{
    private static readonly EventId Evento = new(10306, "IdentidadUniversalDecision");
    private readonly IdentidadUniversalAuthorizationGuard _guard;
    private readonly ILogger<IdentidadUniversalAuditGuard> _logger;

    public IdentidadUniversalAuditGuard(
        IdentidadUniversalAuthorizationGuard guard,
        ILogger<IdentidadUniversalAuditGuard> logger)
    {
        _guard = guard ?? throw new ArgumentNullException(nameof(guard));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ContextoTenantActual> ExigirYAuditarAsync(
        ContextoTenantActual autenticado, int empresaId,
        ModuloSistema modulo, AccionPermiso accion, Guid correlacionId,
        int? sucursalId = null, CancellationToken cancellationToken = default)
    {
        if (correlacionId == Guid.Empty)
            throw new ArgumentException("Se requiere correlacion valida.", nameof(correlacionId));

        try
        {
            // Reusa la guardia de membresia y permiso RELACIONAL vigente.
            // Tenant B se deniega antes de cualquier consulta de la fuente.
            var actual = await _guard.ExigirAccesoActualAsync(
                autenticado, empresaId, modulo, accion, sucursalId, cancellationToken);
            Registrar(LogLevel.Information, "ALLOW", "NONE", correlacionId);
            return actual;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            Registrar(LogLevel.Debug, "CANCELLED", "REQUEST_CANCELLED", correlacionId);
            throw;
        }
        catch (InvalidOperationException)
        {
            Registrar(LogLevel.Warning, "DENY", "ACCESS_DENIED", correlacionId);
            throw;
        }
        catch (ArgumentException)
        {
            Registrar(LogLevel.Warning, "DENY", "INVALID_SCOPE", correlacionId);
            throw;
        }
        catch (Exception)
        {
            // La excepcion permanece para el middleware HTTP con mensaje
            // publico generico; nunca se envia al logger como objeto o texto.
            Registrar(LogLevel.Error, "ERROR", "INTERNAL_ERROR", correlacionId);
            throw;
        }
    }

    private void Registrar(LogLevel nivel, string decision, string codigo, Guid correlacionId)
    {
        // Solo literales controlados y GUID emitido por el servidor.
        // No registrar usuario, empresa, sucursal, JWT, payload ni excepcion.
        _logger.Log(nivel, Evento,
            "IdentidadUniversal {Decision} {ReasonCode} {CorrelationId}",
            decision, codigo, correlacionId);
    }
}

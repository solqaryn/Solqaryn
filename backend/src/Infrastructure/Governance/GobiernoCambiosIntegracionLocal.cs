using Solqaryn.Application.Governance;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Security;

namespace Solqaryn.Infrastructure.Governance;

/// <summary>
/// Resultado no autoritativo de una integración exclusivamente en memoria.
/// La coincidencia local NO es un estado institucional, una autorización, un lease ni un CAS.
/// </summary>
public sealed record DiagnosticoIntegracionGobiernoLocal(
    ResultadoPreparacionCambio Preparacion,
    bool CopiaLocalCoincideConSolicitud)
{
    public bool AutorizaEjecucion => false;
    public bool AutorizaAprobacion => false;
    public bool AutorizaPersistenciaOficial => false;
    public bool AutorizaConcesionPermisos => false;
}

/// <summary>
/// C00-04.M03.05: integra una consulta AUTORIZADA de Application con una proyección
/// local aislada de Infrastructure. Sin API/DI, workers, eventos, cola, proveedor,
/// MySQL, Google Sheets, webhook, schedules o efectos de escritura.
/// La consulta de proyección ocurre siempre DESPUÉS del guard JWT/RBAC/tenant servidor.
/// </summary>
public sealed class GobiernoCambiosIntegracionLocal
{
    private readonly GobiernoCambiosAuthorizationService _autorizacion;
    private readonly GobiernoCambiosProyeccionAislada _proyeccion;

    public GobiernoCambiosIntegracionLocal(
        GobiernoCambiosAuthorizationService autorizacion,
        GobiernoCambiosProyeccionAislada proyeccion)
    {
        _autorizacion = autorizacion ?? throw new ArgumentNullException(nameof(autorizacion));
        _proyeccion = proyeccion ?? throw new ArgumentNullException(nameof(proyeccion));
    }

    public async Task<DiagnosticoIntegracionGobiernoLocal> ConsultarAsync(
        ContextoTenantActual autenticado,
        int empresaId,
        SolicitudPreparacionCambio solicitud,
        int? sucursalId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(autenticado);
        ArgumentNullException.ThrowIfNull(solicitud);

        // La guardia realiza la comprobación de empresa, permiso relacional vigente
        // y validación de la propuesta antes de cualquier consulta a la copia local.
        var preparacion = await _autorizacion.PrepararConsultaAutorizadaAsync(
            autenticado, empresaId, solicitud, sucursalId, cancellationToken);

        // La copia local NO es autoridad: un miss, estado viejo o SHA distinto
        // jamás cambia la evaluación o concede una acción.
        var lectura = _proyeccion.Obtener(preparacion.MicrotareaId, preparacion.RevisionEsperada);
        var coincide = lectura is not null
            && string.Equals(lectura.ShaFuente, preparacion.EvidenciaSha, StringComparison.Ordinal);

        return new DiagnosticoIntegracionGobiernoLocal(preparacion, coincide);
    }
}

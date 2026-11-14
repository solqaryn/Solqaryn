using Solqaryn.Application.Security;
using Solqaryn.Domain.Enums;
using Solqaryn.Domain.Governance;
using Solqaryn.Domain.Security;

namespace Solqaryn.Application.Governance;

/// <summary>
/// C00-04.M03.03: consulta de preparacion de gobierno, limitada al tenant
/// y permiso empresarial vigentes. No aprueba, ejecuta ni persiste cambios.
/// No expone endpoint ni registra DI en esta microtarea.
/// </summary>
public sealed class GobiernoCambiosAuthorizationService
{
    private readonly IdentidadUniversalAuthorizationGuard _guard;
    private readonly GobiernoCambiosPreparacionService _preparacion;

    public GobiernoCambiosAuthorizationService(
        IdentidadUniversalAuthorizationGuard guard,
        GobiernoCambiosPreparacionService preparacion)
    {
        _guard = guard ?? throw new ArgumentNullException(nameof(guard));
        _preparacion = preparacion ?? throw new ArgumentNullException(nameof(preparacion));
    }

    public async Task<ResultadoPreparacionCambio> PrepararConsultaAutorizadaAsync(
        ContextoTenantActual autenticado,
        int empresaId,
        SolicitudPreparacionCambio solicitud,
        int? sucursalId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(autenticado);
        ArgumentNullException.ThrowIfNull(solicitud);

        // Nunca consultar membresias de B usando un contexto verificado de A.
        autenticado.ExigirEmpresa(empresaId);

        // La validez formal de la propuesta se verifica ANTES del I/O.
        // Ni un ID valido ni un rollback constituyen autorizacion de ejecutar.
        var propuesta = _preparacion.Preparar(solicitud);

        // La instantanea actual debe proceder de fuente confiable del servidor;
        // Administrar Configuracion es permiso RELACIONAL explicito, no rol legacy.
        await _guard.ExigirAccesoActualAsync(
            autenticado, empresaId, ModuloSistema.Configuracion,
            AccionPermiso.Administrar, sucursalId, cancellationToken);

        // Se devuelve una proyeccion inerte: la preparacion nunca ejecuta
        // efectos, aunque la consulta de este tenant estuviera autorizada.
        return propuesta;
    }
}

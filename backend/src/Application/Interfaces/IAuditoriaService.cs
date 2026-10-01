using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.Interfaces;

public interface IAuditoriaService
{
    /// Registra una entrada en la bitácora con tolerancia a fallos. Se conserva
    /// para operaciones históricas donde una falla del log no debe cancelar la
    /// operación de negocio. IP, UserAgent y CorrelationId se capturan del
    /// HttpContext actual.
    Task RegistrarAsync(
        ModuloSistema modulo, AccionPermiso accion, string descripcion, int? referenciaId = null,
        string? entidad = null, object? valoresAnteriores = null, object? valoresNuevos = null,
        string? motivo = null, string resultado = "Exito", string? error = null);

    /// Registra una entrada de auditoría crítica y propaga cualquier fallo de
    /// persistencia. Debe usarse dentro de una transacción cuando la operación
    /// de negocio no puede confirmarse sin su evidencia de auditoría.
    Task RegistrarEstrictoAsync(
        ModuloSistema modulo, AccionPermiso accion, string descripcion, int? referenciaId = null,
        string? entidad = null, object? valoresAnteriores = null, object? valoresNuevos = null,
        string? motivo = null, string resultado = "Exito", string? error = null);

    Task<PagedResult<RegistroAuditoriaDto>> GetFilteredAsync(AuditoriaFiltroDto filtro);
}

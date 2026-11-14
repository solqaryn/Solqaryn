using Solqaryn.Domain.Entities;

namespace Solqaryn.Application.Interfaces;

/// <summary>
/// Persistencia especializada del motor de costeo. Los métodos ForUpdate forman
/// parte del contrato de concurrencia de N1.10: una valoración no puede decidir
/// sobre política/capas y confirmarse después con un estado distinto.
/// </summary>
public interface ICosteoInventarioRepository
{
    Task<PoliticaCosteoInventario?> GetPoliticaVigenteAsync(
        int empresaConfiguracionId,
        bool forUpdate = false,
        CancellationToken cancellationToken = default);

    Task<CostoEstandarInventario?> GetCostoEstandarVigenteAsync(
        int productoVarianteId,
        DateTime fechaUtc,
        bool forUpdate = false,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CapaCostoInventario>> GetCapasFifoDisponiblesForUpdateAsync(
        int productoVarianteId,
        int almacenId,
        int? ubicacionAlmacenId,
        CancellationToken cancellationToken = default);

    Task<CapaCostoInventario?> GetCapaByIdForUpdateAsync(
        int capaCostoInventarioId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AsignacionCostoMovimientoInventario>> GetAsignacionesPorMovimientoAsync(
        int movimientoInventarioId,
        bool forUpdate = false,
        CancellationToken cancellationToken = default);

    Task AddPoliticaAsync(
        PoliticaCosteoInventario politica,
        CancellationToken cancellationToken = default);

    Task AddCapaAsync(
        CapaCostoInventario capa,
        CancellationToken cancellationToken = default);

    Task AddCostoEstandarAsync(
        CostoEstandarInventario costoEstandar,
        CancellationToken cancellationToken = default);

    Task AddAsignacionAsync(
        AsignacionCostoMovimientoInventario asignacion,
        CancellationToken cancellationToken = default);

    Task AddVariacionAsync(
        VariacionCostoEstandarInventario variacion,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

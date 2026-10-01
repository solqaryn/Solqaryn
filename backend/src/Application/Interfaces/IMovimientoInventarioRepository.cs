using Solqaryn.Application.DTOs;
using Solqaryn.Domain.Common;
using Solqaryn.Domain.Entities;

namespace Solqaryn.Application.Interfaces;

public sealed record MovimientoInventarioOrigenPersistido(
    int MovimientoId,
    int? CompraId,
    int? VentaId,
    int? ConsumoInsumoId,
    int? AjusteInventarioId = null,
    int? TransferenciaInventarioId = null,
    int? RecepcionCompraId = null);

public interface IMovimientoInventarioRepository
{
    Task AddAsync(MovimientoInventario movimiento);
    Task AddConOrigenTipadoAsync(MovimientoInventario movimiento, OrigenMovimientoInventario origen);
    Task<List<MovimientoInventario>> GetByProductoAsync(int productoId);
    Task<List<MovimientoInventario>> GetFilteredAsync(int? productoId, string? tipo, DateTime? desde, DateTime? hasta);
    Task<(List<MovimientoInventario> Items, int TotalCount)> GetPagedAsync(MovimientoInventarioQueryDto query);
    Task<IReadOnlyDictionary<int, MovimientoInventarioOrigenPersistido>> GetOrigenesTipadosAsync(
        IReadOnlyCollection<int> movimientoIds);
    Task<int?> GetUltimoMovimientoOriginalCompraIdAsync(int compraId);
    Task<bool> ExisteMovimientoPosteriorAsync(
        int ultimoMovimientoOriginalId,
        IReadOnlyCollection<int> productoIds);
    Task<bool> ExisteMovimientoPosteriorRecepcionAsync(int recepcionCompraId);
}

public static class MovimientoInventarioRepositoryExtensions
{
    public static Task AddConOrigenTipadoAsync(
        this IMovimientoInventarioRepository repository,
        MovimientoInventario movimiento,
        OrigenMovimientoInventario origen,
        ContextoFisicoMovimientoInventario contexto)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(movimiento);
        ArgumentNullException.ThrowIfNull(origen);
        ArgumentNullException.ThrowIfNull(contexto);

        movimiento.ProductoVarianteId = contexto.ProductoVarianteId;
        movimiento.AlmacenId = contexto.AlmacenId;
        movimiento.UbicacionAlmacenId = contexto.UbicacionAlmacenId;
        movimiento.CorrelationId = contexto.CorrelationId;

        return repository.AddConOrigenTipadoAsync(movimiento, origen);
    }

    public static Task AddConOrigenTipadoCorrelacionadoAsync(
        this IMovimientoInventarioRepository repository,
        MovimientoInventario movimiento,
        OrigenMovimientoInventario origen,
        string correlationId)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(movimiento);
        ArgumentNullException.ThrowIfNull(origen);

        var normalizado = correlationId?.Trim();
        if (string.IsNullOrWhiteSpace(normalizado))
            throw new ArgumentException("El CorrelationId del movimiento de inventario es obligatorio.", nameof(correlationId));

        if (normalizado.Length > ContextoFisicoMovimientoInventario.MaxCorrelationIdLength)
        {
            throw new ArgumentException(
                $"El CorrelationId no puede exceder {ContextoFisicoMovimientoInventario.MaxCorrelationIdLength} caracteres.",
                nameof(correlationId));
        }

        if (!normalizado.All(EsCaracterSeguroCorrelationId))
            throw new ArgumentException("El CorrelationId contiene caracteres no permitidos.", nameof(correlationId));

        movimiento.CorrelationId = normalizado;
        return repository.AddConOrigenTipadoAsync(movimiento, origen);
    }

    private static bool EsCaracterSeguroCorrelationId(char value) =>
        char.IsAsciiLetterOrDigit(value) || value is '-' or '_' or '.' or ':';
}

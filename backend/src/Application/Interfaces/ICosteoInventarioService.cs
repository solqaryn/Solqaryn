using Solqaryn.Application.DTOs;
using Solqaryn.Domain.Enums;
using Solqaryn.Domain.ValueObjects;

namespace Solqaryn.Application.Interfaces;

/// <summary>
/// Boundary único de valoración de inventario para Compras, Ventas, Ajustes y
/// Transferencias. N1.10.D implementará este contrato; B sólo congela su forma.
/// </summary>
public interface ICosteoInventarioService
{
    Task<MetodoCosteoInventario> GetMetodoActivoAsync(CancellationToken cancellationToken = default);

    Task<ResultadoCosteoInventario> RegistrarEntradaAsync(
        CosteoInventarioEntradaRequest request,
        CancellationToken cancellationToken = default);

    Task<ResultadoCosteoInventario> ValorarSalidaAsync(
        CosteoInventarioSalidaRequest request,
        CancellationToken cancellationToken = default);

    Task<ResultadoCosteoInventario> RevertirAsync(
        CosteoInventarioReversionRequest request,
        CancellationToken cancellationToken = default);
}

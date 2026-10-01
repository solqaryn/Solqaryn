using Solqaryn.Domain.Common;
using Solqaryn.Domain.Entities;

namespace Solqaryn.Application.Interfaces;

/// <summary>
/// Punto único de escritura de Kardex para operaciones ERP-N1.5.
/// Permite persistir correlación durable aun cuando el contrato legado todavía
/// no expone una clave física completa, y utiliza contexto físico autoritativo
/// cuando dicha clave ya está disponible.
/// </summary>
public interface IKardexMovimientoWriter
{
    Task RegistrarCorrelacionadoAsync(
        MovimientoInventario movimiento,
        OrigenMovimientoInventario origen,
        string correlationId);

    Task RegistrarFisicoAsync(
        MovimientoInventario movimiento,
        OrigenMovimientoInventario origen,
        ContextoFisicoMovimientoInventario contexto);
}

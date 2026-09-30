using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Common;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.Services;

/// <summary>
/// Materializa la evidencia de Kardex de una recepción dentro de la misma
/// transacción que actualiza ExistenciaVariante y el estado documental.
/// </summary>
public sealed class RecepcionCompraKardexRegistrar
{
    private readonly IKardexMovimientoWriter _writer;
    private readonly ICurrentUserService _currentUser;

    public RecepcionCompraKardexRegistrar(IKardexMovimientoWriter writer, ICurrentUserService currentUser)
    {
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
    }

    public Task RegistrarConfirmacionAsync(
        RecepcionCompra recepcion,
        IReadOnlyList<RecepcionCompraExistenciaTransicion> transiciones) =>
        RegistrarAsync(recepcion, transiciones, esAnulacion: false);

    public Task RegistrarAnulacionAsync(
        RecepcionCompra recepcion,
        IReadOnlyList<RecepcionCompraExistenciaTransicion> transiciones) =>
        RegistrarAsync(recepcion, transiciones, esAnulacion: true);

    private async Task RegistrarAsync(
        RecepcionCompra recepcion,
        IReadOnlyList<RecepcionCompraExistenciaTransicion> transiciones,
        bool esAnulacion)
    {
        ArgumentNullException.ThrowIfNull(recepcion);
        ArgumentNullException.ThrowIfNull(transiciones);
        if (recepcion.Id <= 0)
            throw new BusinessRuleException("La recepción debe estar persistida antes de registrar Kardex.");

        var origen = OrigenMovimientoInventario.DesdeRecepcionCompra(recepcion.Id);
        foreach (var transicion in transiciones)
        {
            var detalle = ResolverDetalle(recepcion, transicion);
            var operacion = esAnulacion ? "anular" : "confirmar";
            var correlationId = $"recepcion:{recepcion.Id}:{operacion}:{detalle.Id}";
            var contexto = ContextoFisicoMovimientoInventario.Crear(
                transicion.Clave.ProductoVarianteId,
                transicion.Clave.AlmacenId,
                transicion.Clave.UbicacionAlmacenId,
                correlationId);

            var movimiento = new MovimientoInventario
            {
                ProductoId = transicion.ProductoId,
                Tipo = esAnulacion ? TipoMovimientoInventario.Salida : TipoMovimientoInventario.Entrada,
                Causa = esAnulacion ? CausaMovimientoInventario.AnulacionRecepcionCompra : CausaMovimientoInventario.RecepcionCompra,
                Cantidad = transicion.CantidadAceptada,
                StockAnterior = transicion.StockAnterior,
                StockNuevo = transicion.StockNuevo,
                CostoUnitario = detalle.CostoUnitarioSnapshot,
                Descripcion = esAnulacion
                    ? $"Anulación de recepción {recepcion.NumeroRecepcion}"
                    : $"Recepción de mercancía {recepcion.NumeroRecepcion}",
                CreadoPorUsuarioId = _currentUser.UsuarioId,
                CreadoPorNombreUsuario = Normalizar(_currentUser.NombreUsuario),
                Fecha = DateTime.UtcNow
            };

            await _writer.RegistrarFisicoAsync(movimiento, origen, contexto);
        }
    }

    private static RecepcionCompraDetalle ResolverDetalle(
        RecepcionCompra recepcion,
        RecepcionCompraExistenciaTransicion transicion)
    {
        var candidatos = recepcion.Detalles.Where(d =>
            d.ProductoId == transicion.ProductoId &&
            d.ProductoVarianteId == transicion.Clave.ProductoVarianteId &&
            d.AlmacenId == transicion.Clave.AlmacenId &&
            d.UbicacionAlmacenId == transicion.Clave.UbicacionAlmacenId &&
            d.CantidadAceptada == transicion.CantidadAceptada).ToList();

        if (candidatos.Count != 1)
            throw new BusinessRuleException("No fue posible correlacionar de forma unívoca la transición física con el detalle de recepción.");

        var detalle = candidatos[0];
        if (detalle.Id <= 0)
            throw new BusinessRuleException("El detalle de recepción debe estar persistido antes de registrar Kardex.");
        return detalle;
    }

    private static string? Normalizar(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

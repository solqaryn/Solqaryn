using Solqaryn.Application.Common;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Common;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.Services;

/// <summary>
/// Emite Kardex físico correlacionado para transferencias usando exclusivamente
/// el writer canónico de N1.5 y el origen relacional TransferenciaInventarioId.
/// </summary>
public sealed class TransferenciaKardexMovimientoRegistrar
{
    private readonly IKardexMovimientoWriter _writer;

    public TransferenciaKardexMovimientoRegistrar(IKardexMovimientoWriter writer)
    {
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
    }

    public Task RegistrarDespachoAsync(
        TransferenciaInventario transferencia,
        IReadOnlyCollection<TransferenciaInventarioTransitoTransicion> transiciones,
        int usuarioId,
        string? nombreUsuario) =>
        RegistrarFisicosAsync(
            transferencia,
            transiciones.Where(x => x.CantidadFisica < 0),
            KardexCorrelationId.TransferenciaDespachar(transferencia.Id),
            TipoMovimientoInventario.Salida,
            CausaMovimientoInventario.TransferenciaDespacho,
            usuarioId,
            nombreUsuario,
            origenFisico: true,
            cantidadSelector: x => checked(-x.CantidadFisica),
            descripcion: $"Despacho de transferencia {transferencia.Numero} hacia almacén {transferencia.AlmacenDestinoId}.");

    public Task RegistrarRecepcionAsync(
        TransferenciaInventario transferencia,
        IReadOnlyCollection<TransferenciaInventarioTransitoTransicion> transiciones,
        int usuarioId,
        string? nombreUsuario) =>
        RegistrarFisicosAsync(
            transferencia,
            transiciones.Where(x => x.CantidadFisica > 0),
            KardexCorrelationId.TransferenciaRecibir(transferencia.Id),
            TipoMovimientoInventario.Entrada,
            CausaMovimientoInventario.TransferenciaRecepcion,
            usuarioId,
            nombreUsuario,
            origenFisico: false,
            cantidadSelector: x => x.CantidadFisica,
            descripcion: $"Recepción de transferencia {transferencia.Numero} desde almacén {transferencia.AlmacenOrigenId}.");

    public Task RegistrarCancelacionAsync(
        TransferenciaInventario transferencia,
        IReadOnlyCollection<TransferenciaInventarioTransitoTransicion> transiciones,
        int usuarioId,
        string? nombreUsuario) =>
        RegistrarFisicosAsync(
            transferencia,
            transiciones.Where(x => x.CantidadFisica > 0 && x.Clave.AlmacenId == transferencia.AlmacenOrigenId),
            KardexCorrelationId.TransferenciaCancelar(transferencia.Id),
            TipoMovimientoInventario.Entrada,
            CausaMovimientoInventario.TransferenciaCancelacion,
            usuarioId,
            nombreUsuario,
            origenFisico: true,
            cantidadSelector: x => x.CantidadFisica,
            descripcion: $"Reversión por cancelación de transferencia {transferencia.Numero} desde tránsito.");

    private async Task RegistrarFisicosAsync(
        TransferenciaInventario transferencia,
        IEnumerable<TransferenciaInventarioTransitoTransicion> transiciones,
        string correlationId,
        TipoMovimientoInventario tipo,
        CausaMovimientoInventario causa,
        int usuarioId,
        string? nombreUsuario,
        bool origenFisico,
        Func<TransferenciaInventarioTransitoTransicion, int> cantidadSelector,
        string descripcion)
    {
        ValidarTransferenciaPersistida(transferencia, usuarioId);
        var origen = OrigenMovimientoInventario.DesdeTransferenciaInventario(transferencia.Id);

        foreach (var transicion in transiciones)
        {
            var detalle = BuscarDetalle(transferencia, transicion.Clave, origenFisico);
            var movimiento = CrearMovimiento(
                detalle,
                transicion,
                tipo,
                causa,
                cantidadSelector(transicion),
                usuarioId,
                nombreUsuario,
                descripcion);
            var contexto = ContextoFisicoMovimientoInventario.Crear(
                transicion.Clave.ProductoVarianteId,
                transicion.Clave.AlmacenId,
                transicion.Clave.UbicacionAlmacenId,
                correlationId);

            await _writer.RegistrarFisicoAsync(movimiento, origen, contexto);
        }
    }

    private static MovimientoInventario CrearMovimiento(
        TransferenciaInventarioDetalle detalle,
        TransferenciaInventarioTransitoTransicion transicion,
        TipoMovimientoInventario tipo,
        CausaMovimientoInventario causa,
        int cantidad,
        int usuarioId,
        string? nombreUsuario,
        string descripcion) =>
        new()
        {
            ProductoId = detalle.ProductoVariante.ProductoId,
            ProductoVarianteId = detalle.ProductoVarianteId,
            ProductoMarcaSnapshot = detalle.ProductoMarcaSnapshot,
            ProductoModeloSnapshot = detalle.ProductoModeloSnapshot,
            ProductoColorSnapshot = detalle.ProductoColorSnapshot,
            ProductoTallaSnapshot = detalle.ProductoTallaSnapshot,
            ProductoSkuSnapshot = detalle.ProductoSkuSnapshot,
            Tipo = tipo,
            Causa = causa,
            Cantidad = cantidad,
            StockAnterior = transicion.StockFisicoAnterior,
            StockNuevo = transicion.StockFisicoNuevo,
            CostoUnitario = detalle.ProductoVariante.Costo,
            Descripcion = descripcion,
            CreadoPorUsuarioId = usuarioId,
            CreadoPorNombreUsuario = nombreUsuario,
            Fecha = DateTime.UtcNow
        };

    private static TransferenciaInventarioDetalle BuscarDetalle(
        TransferenciaInventario transferencia,
        InventarioExistenciaClave clave,
        bool origen)
    {
        var detalle = transferencia.Detalles.FirstOrDefault(d =>
            d.ProductoVarianteId == clave.ProductoVarianteId &&
            (origen ? d.UbicacionOrigenId : d.UbicacionDestinoId) == clave.UbicacionAlmacenId);
        return detalle ?? throw new BusinessRuleException(
            "No se encontró el detalle de transferencia correspondiente a la transición física de Kardex.");
    }

    private static void ValidarTransferenciaPersistida(TransferenciaInventario transferencia, int usuarioId)
    {
        ArgumentNullException.ThrowIfNull(transferencia);
        if (transferencia.Id <= 0)
            throw new BusinessRuleException("La transferencia debe estar persistida antes de registrar Kardex.");
        if (usuarioId <= 0)
            throw new BusinessRuleException("El usuario de Kardex debe ser válido.");
    }
}

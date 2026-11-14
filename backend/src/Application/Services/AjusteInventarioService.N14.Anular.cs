using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Common;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.Services;

public sealed partial class AjusteInventarioService
{
    public async Task<AjusteInventarioDto?> AnularAsync(int id, string motivoAnulacion)
    {
        if (string.IsNullOrWhiteSpace(motivoAnulacion) || motivoAnulacion.Trim().Length > 500)
            throw new BusinessRuleException("El motivo de anulación es obligatorio y no puede exceder 500 caracteres.");

        var (usuarioId, nombreUsuario) = ObtenerUsuarioActual();
        var motivo = motivoAnulacion.Trim();
        var encontrado = false;

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var ajuste = await _repository.GetByIdForUpdateAsync(id);
            if (ajuste is null) return;
            encontrado = true;

            if (ajuste.Estado != EstadoAjusteInventario.Confirmado)
                throw new BusinessRuleException("Solo los ajustes confirmados pueden anularse.");
            if (ajuste.Detalles.Any(d => !d.TieneSnapshotConfirmacion))
                throw new BusinessRuleException("El ajuste confirmado no posee snapshots íntegros y no puede anularse de forma segura.");

            var cutover = CrearCutoverExistencias();
            var existencias = await cutover.BloquearParaReversionAsync(ajuste.Detalles);

            // Bridge de compatibilidad: estas filas se bloquean únicamente para mantener
            // Producto.Cantidad/ProductoVariante.Cantidad como proyección agregada.
            // Nunca se leen para decidir stock, snapshots ni viabilidad de la reversión.
            // Una variante puede existir en varias filas físicas del ajuste; el bridge
            // agregado se bloquea una sola vez por producto/variante.
            var lockRequest = ajuste.Detalles
                .GroupBy(d => (d.ProductoId, d.ProductoVarianteId))
                .OrderBy(g => g.Key.ProductoId)
                .ThenBy(g => g.Key.ProductoVarianteId)
                .Select(g => new InventarioDemanda(g.Key.ProductoId, g.Key.ProductoVarianteId, 1))
                .ToList();
            var inventarioLegacy = await _inventarioConcurrency.BloquearInventarioParaReversionAsync(lockRequest);
            var correlationId = $"ajuste:{ajuste.Id}:anular:{Guid.NewGuid():N}";

            foreach (var detalle in ajuste.Detalles
                         .OrderBy(d => d.ProductoVarianteId)
                         .ThenBy(d => d.AlmacenId)
                         .ThenBy(d => d.UbicacionAlmacenId))
            {
                if (!inventarioLegacy.Productos.TryGetValue(detalle.ProductoId, out var producto))
                    throw new BusinessRuleException($"El producto ID '{detalle.ProductoId}' ya no existe físicamente.");
                if (!detalle.ProductoVarianteId.HasValue ||
                    !inventarioLegacy.Variantes.TryGetValue(detalle.ProductoVarianteId.Value, out var variante))
                {
                    throw new BusinessRuleException(
                        "N1.4 requiere una variante concreta para revertir existencias de forma segura.");
                }
                if (variante.ProductoId != detalle.ProductoId)
                    throw new BusinessRuleException("La variante histórica ya no pertenece al producto del ajuste.");

                var costoUnitario = detalle.CostoUnitarioSnapshot
                    ?? throw new BusinessRuleException("El ajuste no contiene costo histórico válido para revertir.");
                var transicion = await cutover.AplicarReversionConSnapshotAsync(existencias, detalle);

                SincronizarProyeccionLegacy(producto, variante, transicion.Diferencia, ajuste.NumeroAjuste);

                var contextoKardex = ContextoFisicoMovimientoInventario.Crear(
                    detalle.ProductoVarianteId.Value,
                    detalle.AlmacenId ?? throw new BusinessRuleException("El ajuste no posee almacén físico válido para revertir Kardex."),
                    detalle.UbicacionAlmacenId,
                    correlationId);

                await _movimientoInventarioRepository.AddConOrigenTipadoAsync(
                    new MovimientoInventario
                    {
                        ProductoId = detalle.ProductoId,
                        ProductoVarianteId = detalle.ProductoVarianteId,
                        AlmacenId = detalle.AlmacenId,
                        UbicacionAlmacenId = detalle.UbicacionAlmacenId,
                        ProductoColorSnapshot = detalle.ColorSnapshot,
                        ProductoSkuSnapshot = detalle.SkuSnapshot,
                        Tipo = TipoMovimientoInventario.Reversion,
                        Causa = CausaMovimientoInventario.AjusteManual,
                        Cantidad = Math.Abs(transicion.Diferencia),
                        StockAnterior = transicion.StockAnterior,
                        StockNuevo = transicion.StockNuevo,
                        CostoUnitario = costoUnitario,
                        Descripcion = $"Reversión del ajuste {ajuste.NumeroAjuste}. Motivo: {motivo}",
                        CreadoPorUsuarioId = usuarioId,
                        CreadoPorNombreUsuario = nombreUsuario,
                        Fecha = DateTime.UtcNow
                    },
                    OrigenMovimientoInventario.DesdeAjusteInventario(ajuste.Id),
                    contextoKardex);
            }

            var ahora = DateTime.UtcNow;
            ajuste.Anular(usuarioId, nombreUsuario, motivo, ahora);
            ajuste.ActualizadoPorUsuarioId = usuarioId;
            ajuste.ActualizadoPorNombreUsuario = nombreUsuario;
            ajuste.FechaActualizacion = ahora;
            _repository.Update(ajuste);
            await _repository.SaveChangesAsync();

            await _auditoria.RegistrarEstrictoAsync(
                ModuloSistema.Inventario,
                AccionPermiso.Anular,
                $"Ajuste de inventario anulado: {ajuste.NumeroAjuste}",
                ajuste.Id,
                entidad: nameof(AjusteInventario),
                motivo: motivo);
        });

        if (!encontrado) return null;

        var anulado = await _repository.GetByIdAsync(id)
            ?? throw new InvalidOperationException("No se pudo recuperar el ajuste anulado.");

        return ToDto(anulado);
    }

}

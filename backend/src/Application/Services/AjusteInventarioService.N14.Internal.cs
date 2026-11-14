using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Common;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.Services;

public sealed partial class AjusteInventarioService
{
    private async Task<AjusteInventario> CrearBorradorInternoAsync(
        CreateAjusteInventarioDto dto,
        int usuarioId,
        string nombreUsuario)
    {
        ValidarCabecera(dto.Motivo, dto.Observaciones, dto.Detalles);
        var ahora = DateTime.UtcNow;
        var ajuste = new AjusteInventario
        {
            NumeroAjuste = $"TMP-{Guid.NewGuid():N}"[..20],
            FechaAjuste = dto.FechaAjuste ?? ahora,
            Motivo = dto.Motivo.Trim(),
            Observaciones = NormalizarOpcional(dto.Observaciones),
            CreadoPorUsuarioId = usuarioId,
            CreadoPorNombreUsuario = nombreUsuario,
            ActualizadoPorUsuarioId = usuarioId,
            ActualizadoPorNombreUsuario = nombreUsuario,
            FechaCreacion = ahora,
            FechaActualizacion = ahora
        };

        await ReemplazarDetallesAsync(ajuste, dto.Detalles);
        await _repository.AddAsync(ajuste);
        await _repository.SaveChangesAsync();

        ajuste.NumeroAjuste = $"AI-{ajuste.Id:D6}";
        _repository.Update(ajuste);
        await _repository.SaveChangesAsync();
        return ajuste;
    }

    private async Task ConfirmarInternoAsync(
        AjusteInventario ajuste,
        int usuarioId,
        string nombreUsuario,
        IReadOnlyDictionary<(int ProductoId, int? ProductoVarianteId), int>? cantidadesEsperadas)
    {
        if (ajuste.Estado != EstadoAjusteInventario.Borrador)
            throw new BusinessRuleException("Solo los ajustes en estado Borrador pueden confirmarse.");
        if (ajuste.Detalles.Count == 0)
            throw new BusinessRuleException("El ajuste debe contener al menos un detalle para confirmarse.");

        var cutover = CrearCutoverExistencias();
        var existencias = await cutover.BloquearParaConfirmacionAsync(ajuste.Detalles);

        // Bridge legacy bloqueado después de la autoridad física. Sus cantidades sólo
        // reciben el delta autoritativo y no participan en ninguna decisión de stock.
        // Una variante puede aparecer en varios almacenes/ubicaciones; el bridge agregado
        // debe bloquearse una sola vez por producto/variante para evitar locks redundantes.
        var lockRequest = ajuste.Detalles
            .GroupBy(d => (d.ProductoId, d.ProductoVarianteId))
            .OrderBy(g => g.Key.ProductoId)
            .ThenBy(g => g.Key.ProductoVarianteId)
            .Select(g => new InventarioDemanda(g.Key.ProductoId, g.Key.ProductoVarianteId, 1))
            .ToList();
        var inventarioLegacy = await _inventarioConcurrency.BloquearInventarioParaReversionAsync(lockRequest);

        var productosCompletos = new Dictionary<int, Producto>();
        foreach (var productoId in ajuste.Detalles.Select(d => d.ProductoId).Distinct().OrderBy(x => x))
        {
            productosCompletos[productoId] = await _productoRepository.GetByIdAsync(productoId)
                ?? throw new BusinessRuleException($"El producto ID '{productoId}' ya no está disponible para confirmar el ajuste.");
        }

        var correlationId = $"ajuste:{ajuste.Id}:confirmar:{Guid.NewGuid():N}";
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
                    "N1.4 requiere una variante concreta para ajustar existencias de forma segura.");
            }
            if (variante.ProductoId != detalle.ProductoId)
                throw new BusinessRuleException("La variante indicada ya no pertenece al producto del ajuste.");
            if (variante.Eliminado)
                throw new BusinessRuleException($"La variante '{variante.Sku}' fue eliminada y no puede ajustarse.");

            var productoCompleto = productosCompletos[detalle.ProductoId];
            var existencia = AjusteInventarioExistenciaStock.ObtenerExistencia(existencias, detalle);
            var cantidadAnterior = existencia.StockFisico;
            var costoUnitario = variante.Costo ?? producto.Costo;

            var key = (detalle.ProductoId, detalle.ProductoVarianteId);
            if (cantidadesEsperadas is not null &&
                cantidadesEsperadas.TryGetValue(key, out var cantidadEsperada) &&
                cantidadEsperada != cantidadAnterior)
            {
                throw new BusinessRuleException(
                    $"El stock físico autoritativo cambió desde la lectura del cliente. Esperado: {cantidadEsperada}; actual: {cantidadAnterior}. Actualiza la información y vuelve a intentar.");
            }

            detalle.MaterializarConfirmacion(cantidadAnterior, costoUnitario);
            AplicarSnapshotsIdentidad(detalle, productoCompleto, variante);

            var transicion = await cutover.AplicarConfirmacionConSnapshotAsync(existencias, detalle);
            SincronizarProyeccionLegacy(producto, variante, transicion.Diferencia, ajuste.NumeroAjuste);

            var contextoKardex = ContextoFisicoMovimientoInventario.Crear(
                detalle.ProductoVarianteId.Value,
                detalle.AlmacenId ?? throw new BusinessRuleException("El ajuste no posee almacén físico válido para registrar Kardex."),
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
                    Tipo = TipoMovimientoInventario.Ajuste,
                    Causa = CausaMovimientoInventario.AjusteManual,
                    Cantidad = Math.Abs(transicion.Diferencia),
                    StockAnterior = transicion.StockAnterior,
                    StockNuevo = transicion.StockNuevo,
                    CostoUnitario = costoUnitario,
                    Descripcion = $"Ajuste formal de inventario {ajuste.NumeroAjuste}. Motivo: {ajuste.Motivo}",
                    CreadoPorUsuarioId = usuarioId,
                    CreadoPorNombreUsuario = nombreUsuario,
                    Fecha = DateTime.UtcNow
                },
                OrigenMovimientoInventario.DesdeAjusteInventario(ajuste.Id),
                contextoKardex);
        }

        var ahora = DateTime.UtcNow;
        ajuste.Confirmar(usuarioId, nombreUsuario, ahora);
        ajuste.ActualizadoPorUsuarioId = usuarioId;
        ajuste.ActualizadoPorNombreUsuario = nombreUsuario;
        ajuste.FechaActualizacion = ahora;
        _repository.Update(ajuste);
        await _repository.SaveChangesAsync();

        await _auditoria.RegistrarEstrictoAsync(
            ModuloSistema.Inventario,
            AccionPermiso.Confirmar,
            $"Ajuste de inventario confirmado: {ajuste.NumeroAjuste}",
            ajuste.Id,
            entidad: nameof(AjusteInventario));
    }

    private async Task ReemplazarDetallesAsync(
        AjusteInventario ajuste,
        IReadOnlyCollection<AjusteInventarioDetalleInputDto> detalles)
    {
        var duplicado = detalles
            .GroupBy(d => (d.ProductoId, d.ProductoVarianteId, d.AlmacenId, d.UbicacionAlmacenId))
            .FirstOrDefault(g => g.Count() > 1);
        if (duplicado is not null)
            throw new BusinessRuleException("Cada producto/variante/almacén/ubicación puede aparecer una sola vez en el ajuste.");

        foreach (var entrada in detalles
            .OrderBy(d => d.ProductoId)
            .ThenBy(d => d.ProductoVarianteId)
            .ThenBy(d => d.AlmacenId)
            .ThenBy(d => d.UbicacionAlmacenId))
        {
            var producto = await _productoRepository.GetByIdAsync(entrada.ProductoId)
                ?? throw new BusinessRuleException($"El producto ID '{entrada.ProductoId}' no existe.");

            var variantes = (producto.Variantes ?? Array.Empty<ProductoVariante>())
                .Where(v => !v.Eliminado)
                .ToList();

            if (entrada.ProductoVarianteId.HasValue)
            {
                var variante = variantes.FirstOrDefault(v => v.Id == entrada.ProductoVarianteId.Value)
                    ?? throw new BusinessRuleException("La variante indicada no pertenece al producto seleccionado.");
            }
            else if (variantes.Count > 0)
            {
                throw new BusinessRuleException(
                    $"El producto '{producto.Nombre}' posee variantes. Selecciona la variante concreta que deseas ajustar.");
            }

            ajuste.Detalles.Add(new AjusteInventarioDetalle
            {
                ProductoId = entrada.ProductoId,
                ProductoVarianteId = entrada.ProductoVarianteId,
                AlmacenId = entrada.AlmacenId > 0 ? entrada.AlmacenId : null,
                UbicacionAlmacenId = entrada.UbicacionAlmacenId,
                CantidadObjetivo = entrada.CantidadObjetivo,
                FechaCreacion = DateTime.UtcNow,
                FechaActualizacion = DateTime.UtcNow
            });
        }
    }

}

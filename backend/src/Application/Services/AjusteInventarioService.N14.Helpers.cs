using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Common;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.Services;

public sealed partial class AjusteInventarioService
{
    private static void ValidarCabecera(
        string motivo,
        string? observaciones,
        IReadOnlyCollection<AjusteInventarioDetalleInputDto> detalles)
    {
        if (string.IsNullOrWhiteSpace(motivo) || motivo.Trim().Length > 500)
            throw new BusinessRuleException("El motivo es obligatorio y no puede exceder 500 caracteres.");
        if (observaciones?.Trim().Length > 1000)
            throw new BusinessRuleException("Las observaciones no pueden exceder 1000 caracteres.");
        if (detalles is null || detalles.Count == 0)
            throw new BusinessRuleException("El ajuste debe contener al menos un detalle.");
        if (detalles.Count > 200)
            throw new BusinessRuleException("El ajuste no puede contener más de 200 líneas.");
        if (detalles.Any(d =>
                d.ProductoId <= 0 ||
                !d.ProductoVarianteId.HasValue || d.ProductoVarianteId.Value <= 0 ||
                d.AlmacenId <= 0 ||
                (d.UbicacionAlmacenId.HasValue && d.UbicacionAlmacenId.Value <= 0) ||
                d.CantidadObjetivo < 0))
        {
            throw new BusinessRuleException(
                "Cada línea debe indicar producto, variante y almacén válidos, ubicación positiva cuando aplique y una cantidad objetivo no negativa.");
        }
    }

    private static void ValidarSolicitudCompatibilidad(
        int productoId,
        int? varianteId,
        AjusteStockRequest request)
    {
        if (productoId <= 0 || !varianteId.HasValue || varianteId.Value <= 0)
            throw new BusinessRuleException("N1.4 requiere producto y variante concretos para ajustar stock.");
        if (request.AlmacenId <= 0 ||
            (request.UbicacionAlmacenId.HasValue && request.UbicacionAlmacenId.Value <= 0))
        {
            throw new BusinessRuleException(
                "El ajuste debe indicar un almacén válido y una ubicación positiva cuando aplique; no se infiere contexto físico.");
        }
        if (request.CantidadActualEsperada < 0 || request.CantidadNueva < 0)
            throw new BusinessRuleException("Las cantidades de inventario no pueden ser negativas.");
        if (string.IsNullOrWhiteSpace(request.Motivo))
            throw new BusinessRuleException("El motivo del ajuste de inventario es obligatorio.");
        if (request.CantidadActualEsperada == request.CantidadNueva)
            throw new BusinessRuleException("La nueva cantidad debe ser diferente del stock actual.");
    }

    private AjusteInventarioExistenciaCutoverService CrearCutoverExistencias() =>
        new(_existenciaVarianteConcurrency
            ?? throw new InvalidOperationException(
                "N1.4.D requiere IExistenciaVarianteConcurrencyService para operar stock autoritativo."));

    private void SincronizarProyeccionLegacy(
        Producto producto,
        ProductoVariante variante,
        int diferenciaAutoritativa,
        string referencia)
    {
        int nuevaVariante;
        int nuevoProducto;
        try
        {
            nuevaVariante = checked(variante.Cantidad + diferenciaAutoritativa);
            nuevoProducto = checked(producto.Cantidad + diferenciaAutoritativa);
        }
        catch (OverflowException ex)
        {
            throw new BusinessRuleException(
                $"La proyección legacy de {referencia} excede el rango soportado: {ex.Message}");
        }

        if (nuevaVariante < 0 || nuevoProducto < 0)
        {
            throw new BusinessRuleException(
                $"La proyección legacy de {referencia} está inconsistente con ExistenciaVariante y no puede sincronizarse de forma segura.");
        }

        variante.Cantidad = nuevaVariante;
        producto.Cantidad = nuevoProducto;
        _productoVarianteRepository.Update(variante);
        _productoRepository.Update(producto);
    }

    private (int UsuarioId, string NombreUsuario) ObtenerUsuarioActual()
    {
        var usuarioId = _currentUser.UsuarioId;
        var nombreUsuario = _currentUser.NombreUsuario?.Trim();
        if (!usuarioId.HasValue || usuarioId.Value <= 0 || string.IsNullOrWhiteSpace(nombreUsuario))
            throw new BusinessRuleException("No fue posible identificar al usuario autenticado para registrar el ajuste.");
        return (usuarioId.Value, nombreUsuario);
    }

    private static void AplicarSnapshotsIdentidad(
        AjusteInventarioDetalle detalle,
        Producto producto,
        ProductoVariante? variante)
    {
        var varianteCompleta = detalle.ProductoVarianteId.HasValue
            ? producto.Variantes.FirstOrDefault(v => v.Id == detalle.ProductoVarianteId.Value)
            : null;

        detalle.NombreSnapshot = producto.Nombre;
        detalle.SkuSnapshot = varianteCompleta?.Sku ?? variante?.Sku;
        detalle.MarcaSnapshot = varianteCompleta?.Marca?.Nombre ?? producto.Marca;
        detalle.ModeloSnapshot = varianteCompleta?.Modelo?.Nombre ?? producto.Modelo;
        detalle.ColorSnapshot = varianteCompleta?.Color?.Nombre ?? producto.Color?.Nombre;
        detalle.TallaSnapshot = varianteCompleta?.Talla?.Nombre ?? producto.Talla?.Nombre;
        detalle.FechaActualizacion = DateTime.UtcNow;
    }

    private static string? NormalizarOpcional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static AjusteInventarioDto ToDto(AjusteInventario ajuste) => new()
    {
        Id = ajuste.Id,
        NumeroAjuste = ajuste.NumeroAjuste,
        FechaAjuste = ajuste.FechaAjuste,
        Estado = ajuste.Estado.ToString(),
        Motivo = ajuste.Motivo,
        Observaciones = ajuste.Observaciones,
        FechaConfirmacion = ajuste.FechaConfirmacion,
        ConfirmadoPorNombreUsuario = ajuste.ConfirmadoPorNombreUsuario,
        FechaAnulacion = ajuste.FechaAnulacion,
        AnuladoPorNombreUsuario = ajuste.AnuladoPorNombreUsuario,
        MotivoAnulacion = ajuste.MotivoAnulacion,
        ImpactoCostoTotalSnapshot = ajuste.Detalles
            .Where(d => d.ImpactoCostoSnapshot.HasValue)
            .Sum(d => d.ImpactoCostoSnapshot ?? 0m),
        Detalles = ajuste.Detalles
            .OrderBy(d => d.Id)
            .Select(d => new AjusteInventarioDetalleDto
            {
                Id = d.Id,
                ProductoId = d.ProductoId,
                ProductoVarianteId = d.ProductoVarianteId,
                AlmacenId = d.AlmacenId ?? 0,
                UbicacionAlmacenId = d.UbicacionAlmacenId,
                CantidadObjetivo = d.CantidadObjetivo,
                CantidadAnteriorSnapshot = d.CantidadAnteriorSnapshot,
                CantidadNuevaSnapshot = d.CantidadNuevaSnapshot,
                DiferenciaSnapshot = d.DiferenciaSnapshot,
                CostoUnitarioSnapshot = d.CostoUnitarioSnapshot,
                ImpactoCostoSnapshot = d.ImpactoCostoSnapshot,
                NombreSnapshot = d.NombreSnapshot,
                SkuSnapshot = d.SkuSnapshot,
                MarcaSnapshot = d.MarcaSnapshot,
                ModeloSnapshot = d.ModeloSnapshot,
                ColorSnapshot = d.ColorSnapshot,
                TallaSnapshot = d.TallaSnapshot
            })
            .ToList()
    };
}

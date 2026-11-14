using Solqaryn.Application.DTOs.Contabilidad;
using Solqaryn.Application.Exceptions;
using Solqaryn.Domain.Entities.Contabilidad;

namespace Solqaryn.Application.Services;

public static class AsientoContableApplicationService
{
    public static AsientoContable CrearAggregate(CrearAsientoContableDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var concepto = dto.Concepto?.Trim();
        if (string.IsNullOrWhiteSpace(concepto))
            throw new BusinessRuleException("El concepto del asiento es obligatorio.");
        if (dto.Detalles is null || dto.Detalles.Count < 2)
            throw new BusinessRuleException("El asiento debe contener al menos dos detalles.");

        var asiento = new AsientoContable
        {
            Fecha = dto.Fecha ?? DateTime.UtcNow,
            Concepto = concepto,
            Numero = NormalizeOptional(dto.Numero),
            DocumentoOrigenId = dto.DocumentoOrigenId,
            TipoDocumentoOrigen = NormalizeOptional(dto.TipoDocumentoOrigen)
        };

        foreach (var detalle in dto.Detalles)
        {
            asiento.AgregarDetalle(new AsientoDetalle(
                detalle.CuentaContableId,
                detalle.Debe,
                detalle.Haber,
                NormalizeOptional(detalle.Referencia)));
        }

        asiento.ValidarCuadre();
        return asiento;
    }

    public static AsientoContableDto Mapear(AsientoContable entity)
    {
        var detalles = entity.Detalles.Select(d => new AsientoDetalleDto
        {
            Id = d.Id,
            CuentaContableId = d.CuentaContableId,
            CuentaCodigo = d.CuentaContable?.Codigo,
            CuentaNombre = d.CuentaContable?.Nombre,
            Debe = d.Debe,
            Haber = d.Haber,
            Referencia = d.Referencia
        }).ToList();

        return new AsientoContableDto
        {
            Id = entity.Id,
            Fecha = entity.Fecha,
            Concepto = entity.Concepto,
            Numero = entity.Numero,
            DocumentoOrigenId = entity.DocumentoOrigenId,
            TipoDocumentoOrigen = entity.TipoDocumentoOrigen,
            TotalDebe = detalles.Sum(d => d.Debe),
            TotalHaber = detalles.Sum(d => d.Haber),
            Detalles = detalles
        };
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
